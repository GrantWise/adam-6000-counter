using Industrial.Adam.EquipmentScheduling.Domain.Entities;
using Industrial.Adam.EquipmentScheduling.Domain.Enums;
using Industrial.Adam.EquipmentScheduling.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.EquipmentScheduling.Domain.Services;

/// <summary>
/// Domain service for generating equipment schedules from operating patterns
/// </summary>
public sealed class ScheduleGenerationService
{
    private readonly IPatternAssignmentRepository _patternAssignmentRepository;
    private readonly IOperatingPatternRepository _operatingPatternRepository;
    private readonly ILogger<ScheduleGenerationService> _logger;

    public ScheduleGenerationService(
        IPatternAssignmentRepository patternAssignmentRepository,
        IOperatingPatternRepository operatingPatternRepository,
        ILogger<ScheduleGenerationService> logger)
    {
        _patternAssignmentRepository = patternAssignmentRepository ?? throw new ArgumentNullException(nameof(patternAssignmentRepository));
        _operatingPatternRepository = operatingPatternRepository ?? throw new ArgumentNullException(nameof(operatingPatternRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generates schedules for a resource within a date range
    /// </summary>
    /// <param name="resourceId">The resource identifier</param>
    /// <param name="startDate">The start date</param>
    /// <param name="endDate">The end date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of generated schedules</returns>
    public async Task<IEnumerable<EquipmentSchedule>> GenerateSchedulesAsync(
        long resourceId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating schedules for resource {ResourceId} from {StartDate} to {EndDate}",
            resourceId, startDate.Date, endDate.Date);

        var schedules = new List<EquipmentSchedule>();
        var currentDate = startDate.Date;

        while (currentDate <= endDate.Date)
        {
            var assignment = await _patternAssignmentRepository.GetActiveAssignmentAsync(
                resourceId, currentDate, cancellationToken);

            if (assignment != null)
            {
                var pattern = await _operatingPatternRepository.GetByIdAsync(
                    assignment.PatternId, cancellationToken);

                if (pattern != null)
                {
                    var dailySchedules = GenerateDailySchedules(
                        resourceId, currentDate, pattern, assignment);
                    schedules.AddRange(dailySchedules);
                }
                else
                {
                    _logger.LogWarning("Pattern {PatternId} not found for assignment {AssignmentId}",
                        assignment.PatternId, assignment.Id);
                }
            }

            currentDate = currentDate.AddDays(1);
        }

        _logger.LogInformation("Generated {ScheduleCount} schedules for resource {ResourceId}",
            schedules.Count, resourceId);

        return schedules;
    }

    /// <summary>
    /// Generates daily schedules for a specific date based on operating pattern
    /// </summary>
    /// <param name="resourceId">The resource identifier</param>
    /// <param name="date">The date to generate schedules for</param>
    /// <param name="pattern">The operating pattern</param>
    /// <param name="assignment">The pattern assignment</param>
    /// <returns>List of daily schedules</returns>
    public IEnumerable<EquipmentSchedule> GenerateDailySchedules(
        long resourceId,
        DateTime date,
        OperatingPattern pattern,
        PatternAssignment assignment)
    {
        var schedules = new List<EquipmentSchedule>();
        var dayOfWeek = date.DayOfWeek;

        // Get the planned hours for this day
        var dailyHours = pattern.GetDailyHours(dayOfWeek);

        if (dailyHours <= 0)
        {
            // No operation planned for this day
            return schedules;
        }

        // Generate schedules based on pattern type
        switch (pattern.Type)
        {
            case PatternType.Continuous:
                schedules.Add(CreateContinuousSchedule(resourceId, date, pattern, assignment));
                break;

            case PatternType.DayOnly:
                var dayShift = CreateShiftSchedule(resourceId, date, pattern, assignment,
                    "DAY", TimeSpan.FromHours(8), TimeSpan.FromHours(16), 8.0m);
                if (dayShift != null)
                    schedules.Add(dayShift);
                break;

            case PatternType.TwoShift:
                if (dayOfWeek != DayOfWeek.Saturday && dayOfWeek != DayOfWeek.Sunday)
                {
                    var dayShift2 = CreateShiftSchedule(resourceId, date, pattern, assignment,
                        "DAY", TimeSpan.FromHours(6), TimeSpan.FromHours(14), 8.0m);
                    var eveningShift = CreateShiftSchedule(resourceId, date, pattern, assignment,
                        "EVE", TimeSpan.FromHours(14), TimeSpan.FromHours(22), 8.0m);

                    if (dayShift2 != null)
                        schedules.Add(dayShift2);
                    if (eveningShift != null)
                        schedules.Add(eveningShift);
                }
                break;

            case PatternType.Extended:
                if (dayOfWeek != DayOfWeek.Saturday && dayOfWeek != DayOfWeek.Sunday)
                {
                    var extendedShift = CreateShiftSchedule(resourceId, date, pattern, assignment,
                        "EXT", TimeSpan.FromHours(6), TimeSpan.FromHours(18), 12.0m);
                    if (extendedShift != null)
                        schedules.Add(extendedShift);
                }
                break;

            case PatternType.Custom:
                var customSchedules = GenerateCustomSchedules(resourceId, date, pattern, assignment);
                schedules.AddRange(customSchedules);
                break;
        }

        return schedules;
    }

    /// <summary>
    /// Creates a continuous operation schedule (24 hours)
    /// </summary>
    private EquipmentSchedule CreateContinuousSchedule(
        long resourceId,
        DateTime date,
        OperatingPattern pattern,
        PatternAssignment assignment)
    {
        var startTime = date.Date;
        var endTime = date.Date.AddDays(1);

        return new EquipmentSchedule(
            resourceId: resourceId,
            scheduleDate: date,
            plannedHours: 24.0m,
            patternId: pattern.Id,
            shiftCode: "24HR",
            plannedStartTime: startTime,
            plannedEndTime: endTime,
            isException: assignment.IsOverride);
    }

    /// <summary>
    /// Creates a shift-based schedule
    /// </summary>
    private EquipmentSchedule? CreateShiftSchedule(
        long resourceId,
        DateTime date,
        OperatingPattern pattern,
        PatternAssignment assignment,
        string shiftCode,
        TimeSpan startTime,
        TimeSpan endTime,
        decimal plannedHours)
    {
        var shiftStartTime = date.Date.Add(startTime);
        var shiftEndTime = date.Date.Add(endTime);

        // Handle shifts that cross midnight
        if (endTime < startTime)
        {
            shiftEndTime = shiftEndTime.AddDays(1);
        }

        return new EquipmentSchedule(
            resourceId: resourceId,
            scheduleDate: date,
            plannedHours: plannedHours,
            patternId: pattern.Id,
            shiftCode: shiftCode,
            plannedStartTime: shiftStartTime,
            plannedEndTime: shiftEndTime,
            isException: assignment.IsOverride);
    }

    /// <summary>
    /// Generates custom schedules based on pattern configuration
    /// </summary>
    private IEnumerable<EquipmentSchedule> GenerateCustomSchedules(
        long resourceId,
        DateTime date,
        OperatingPattern pattern,
        PatternAssignment assignment)
    {
        var schedules = new List<EquipmentSchedule>();

        try
        {
            // Parse the custom pattern configuration
            // This is a simplified implementation - in reality, this would parse
            // the JSON configuration to create the appropriate schedules

            var dailyHours = pattern.GetDailyHours(date.DayOfWeek);
            if (dailyHours > 0)
            {
                var schedule = new EquipmentSchedule(
                    resourceId: resourceId,
                    scheduleDate: date,
                    plannedHours: dailyHours,
                    patternId: pattern.Id,
                    shiftCode: "CUSTOM",
                    isException: assignment.IsOverride);

                schedules.Add(schedule);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate custom schedules for resource {ResourceId} on {Date}",
                resourceId, date);
        }

        return schedules;
    }

    /// <summary>
    /// Validates that generated schedules don't conflict
    /// </summary>
    /// <param name="schedules">The schedules to validate</param>
    /// <returns>List of validation errors</returns>
    public IEnumerable<string> ValidateSchedules(IEnumerable<EquipmentSchedule> schedules)
    {
        var errors = new List<string>();
        var scheduleList = schedules.ToList();

        for (int i = 0; i < scheduleList.Count; i++)
        {
            for (int j = i + 1; j < scheduleList.Count; j++)
            {
                if (scheduleList[i].ConflictsWith(scheduleList[j]))
                {
                    errors.Add($"Schedule conflict between {scheduleList[i].ShiftCode} and {scheduleList[j].ShiftCode} on {scheduleList[i].ScheduleDate:yyyy-MM-dd}");
                }
            }
        }

        return errors;
    }
}
