using Industrial.Adam.EquipmentScheduling.Application.DTOs;
using Industrial.Adam.EquipmentScheduling.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.EquipmentScheduling.Application.Queries.Handlers;

/// <summary>
/// Handler for equipment availability queries
/// </summary>
public sealed class GetAvailabilityQueryHandler :
    IRequestHandler<GetEquipmentAvailabilityQuery, ScheduleAvailabilityDto>,
    IRequestHandler<GetEquipmentSchedulesQuery, IEnumerable<EquipmentScheduleDto>>,
    IRequestHandler<GetDailyScheduleSummaryQuery, DailyScheduleSummaryDto?>,
    IRequestHandler<GetCurrentActiveSchedulesQuery, IEnumerable<EquipmentScheduleDto>>,
    IRequestHandler<GetScheduleConflictsQuery, IEnumerable<string>>,
    IRequestHandler<GetMissingSchedulesQuery, IEnumerable<(long ResourceId, DateTime Date)>>
{
    private readonly IEquipmentScheduleRepository _scheduleRepository;
    private readonly IResourceRepository _resourceRepository;
    private readonly ILogger<GetAvailabilityQueryHandler> _logger;

    public GetAvailabilityQueryHandler(
        IEquipmentScheduleRepository scheduleRepository,
        IResourceRepository resourceRepository,
        ILogger<GetAvailabilityQueryHandler> logger)
    {
        _scheduleRepository = scheduleRepository ?? throw new ArgumentNullException(nameof(scheduleRepository));
        _resourceRepository = resourceRepository ?? throw new ArgumentNullException(nameof(resourceRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ScheduleAvailabilityDto> Handle(GetEquipmentAvailabilityQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting equipment availability for resource {ResourceId} from {StartDate} to {EndDate}",
            request.ResourceId, request.StartDate, request.EndDate);

        // Get resource information
        var resource = await _resourceRepository.GetByIdAsync(request.ResourceId, cancellationToken);
        if (resource == null)
        {
            throw new ArgumentException($"Resource with ID {request.ResourceId} not found", nameof(request.ResourceId));
        }

        // Get schedules for the date range
        var schedules = await _scheduleRepository.GetByResourceAndDateRangeAsync(
            request.ResourceId,
            request.StartDate,
            request.EndDate,
            cancellationToken);

        var scheduleList = schedules.ToList();

        // Calculate metrics
        var totalPlannedHours = scheduleList.Sum(s => s.PlannedHours);
        var scheduledDays = scheduleList.Select(s => s.ScheduleDate).Distinct().Count();
        var totalDays = (int)(request.EndDate.Date - request.StartDate.Date).TotalDays + 1;

        // Calculate theoretical hours based on 24-hour days
        var totalAvailableHours = totalDays * 24m;
        var availabilityPercentage = totalAvailableHours > 0 ? (totalPlannedHours / totalAvailableHours) * 100 : 0;

        var schedulesDtos = scheduleList.Select(s => new EquipmentScheduleDto
        {
            Id = s.Id,
            ResourceId = s.ResourceId,
            ScheduleDate = s.ScheduleDate,
            ShiftCode = s.ShiftCode,
            PlannedStartTime = s.PlannedStartTime,
            PlannedEndTime = s.PlannedEndTime,
            PlannedHours = s.PlannedHours,
            ScheduleStatus = s.Status,
            PatternId = s.PatternId,
            IsException = s.IsException,
            GeneratedAt = s.GeneratedAt
        }).ToList();

        return new ScheduleAvailabilityDto
        {
            ResourceId = request.ResourceId,
            ResourceName = resource.Name,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TotalPlannedHours = totalPlannedHours,
            TotalAvailableHours = totalAvailableHours,
            AvailabilityPercentage = availabilityPercentage,
            ScheduledDays = scheduledDays,
            TotalDays = totalDays,
            Schedules = schedulesDtos
        };
    }

    public async Task<IEnumerable<EquipmentScheduleDto>> Handle(GetEquipmentSchedulesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting equipment schedules for resource {ResourceId} from {StartDate} to {EndDate}",
            request.ResourceId, request.StartDate, request.EndDate);

        var schedules = await _scheduleRepository.GetByResourceAndDateRangeAsync(
            request.ResourceId,
            request.StartDate,
            request.EndDate,
            cancellationToken);

        return schedules.Select(s => new EquipmentScheduleDto
        {
            Id = s.Id,
            ResourceId = s.ResourceId,
            ScheduleDate = s.ScheduleDate,
            ShiftCode = s.ShiftCode,
            PlannedStartTime = s.PlannedStartTime,
            PlannedEndTime = s.PlannedEndTime,
            PlannedHours = s.PlannedHours,
            ScheduleStatus = s.Status,
            PatternId = s.PatternId,
            IsException = s.IsException,
            GeneratedAt = s.GeneratedAt
        });
    }

    public async Task<DailyScheduleSummaryDto?> Handle(GetDailyScheduleSummaryQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting daily schedule summary for resource {ResourceId} on {Date}",
            request.ResourceId, request.Date);

        // Get resource information
        var resource = await _resourceRepository.GetByIdAsync(request.ResourceId, cancellationToken);
        if (resource == null)
        {
            return null;
        }

        // Get schedules for the specific date
        var schedules = await _scheduleRepository.GetByResourceAndDateAsync(
            request.ResourceId,
            request.Date,
            cancellationToken);

        var scheduleList = schedules.ToList();
        if (!scheduleList.Any())
        {
            return null;
        }

        var totalPlannedHours = scheduleList.Sum(s => s.PlannedHours);
        var hasExceptions = scheduleList.Any(s => s.IsException);

        var schedulesDtos = scheduleList.Select(s => new EquipmentScheduleDto
        {
            Id = s.Id,
            ResourceId = s.ResourceId,
            ScheduleDate = s.ScheduleDate,
            ShiftCode = s.ShiftCode,
            PlannedStartTime = s.PlannedStartTime,
            PlannedEndTime = s.PlannedEndTime,
            PlannedHours = s.PlannedHours,
            ScheduleStatus = s.Status,
            PatternId = s.PatternId,
            IsException = s.IsException,
            GeneratedAt = s.GeneratedAt
        }).ToList();

        return new DailyScheduleSummaryDto
        {
            Date = request.Date,
            ResourceId = request.ResourceId,
            ResourceName = resource.Name,
            TotalPlannedHours = totalPlannedHours,
            ScheduleCount = scheduleList.Count,
            HasExceptions = hasExceptions,
            Schedules = schedulesDtos
        };
    }

    public async Task<IEnumerable<EquipmentScheduleDto>> Handle(GetCurrentActiveSchedulesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting current active schedules, resourceId: {ResourceId}", request.ResourceId);

        var currentTime = DateTime.UtcNow;
        var schedules = await _scheduleRepository.GetActiveSchedulesAtTimeAsync(
            currentTime,
            request.ResourceId,
            cancellationToken);

        return schedules.Select(s => new EquipmentScheduleDto
        {
            Id = s.Id,
            ResourceId = s.ResourceId,
            ScheduleDate = s.ScheduleDate,
            ShiftCode = s.ShiftCode,
            PlannedStartTime = s.PlannedStartTime,
            PlannedEndTime = s.PlannedEndTime,
            PlannedHours = s.PlannedHours,
            ScheduleStatus = s.Status,
            PatternId = s.PatternId,
            IsException = s.IsException,
            GeneratedAt = s.GeneratedAt
        });
    }

    public async Task<IEnumerable<string>> Handle(GetScheduleConflictsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting schedule conflicts for resource {ResourceId} from {StartDate} to {EndDate}",
            request.ResourceId, request.StartDate, request.EndDate);

        var conflicts = new List<string>();

        // Check each day in the range for conflicts
        for (var date = request.StartDate.Date; date <= request.EndDate.Date; date = date.AddDays(1))
        {
            var daySchedules = await _scheduleRepository.GetByResourceAndDateAsync(
                request.ResourceId,
                date,
                cancellationToken);

            var dayScheduleList = daySchedules.OrderBy(s => s.PlannedStartTime).ToList();

            // Check for overlapping schedules
            for (int i = 0; i < dayScheduleList.Count - 1; i++)
            {
                var current = dayScheduleList[i];
                var next = dayScheduleList[i + 1];

                if (current.PlannedEndTime > next.PlannedStartTime)
                {
                    conflicts.Add($"Schedule conflict on {date:yyyy-MM-dd}: " +
                        $"Schedule {current.Id} ({current.PlannedStartTime:HH:mm}-{current.PlannedEndTime:HH:mm}) " +
                        $"overlaps with Schedule {next.Id} ({next.PlannedStartTime:HH:mm}-{next.PlannedEndTime:HH:mm})");
                }
            }
        }

        return conflicts;
    }

    public async Task<IEnumerable<(long ResourceId, DateTime Date)>> Handle(GetMissingSchedulesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting missing schedules from {StartDate} to {EndDate}, resourceId: {ResourceId}",
            request.StartDate, request.EndDate, request.ResourceId);

        return await _scheduleRepository.GetMissingSchedulesAsync(
            request.StartDate,
            request.EndDate,
            request.ResourceId,
            cancellationToken);
    }
}
