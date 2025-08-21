using Industrial.Adam.EquipmentScheduling.Domain.Entities;
using Industrial.Adam.EquipmentScheduling.Domain.Enums;
using Industrial.Adam.EquipmentScheduling.Domain.Interfaces;
using Industrial.Adam.EquipmentScheduling.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.EquipmentScheduling.Infrastructure.Repositories;

/// <summary>
/// Entity Framework implementation of IEquipmentScheduleRepository
/// </summary>
public sealed class EquipmentScheduleRepository : IEquipmentScheduleRepository
{
    private readonly EquipmentSchedulingDbContext _context;
    private readonly ILogger<EquipmentScheduleRepository> _logger;

    public EquipmentScheduleRepository(
        EquipmentSchedulingDbContext context,
        ILogger<EquipmentScheduleRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<EquipmentSchedule?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting equipment schedule by ID {ScheduleId}", id);

        return await _context.EquipmentSchedules
            .Include(s => s.Resource)
            .Include(s => s.OperatingPattern)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<EquipmentSchedule>> GetByResourceAndDateAsync(long resourceId, DateTime date, CancellationToken cancellationToken = default)
    {
        var scheduleDate = date.Date;
        _logger.LogDebug("Getting equipment schedules for resource {ResourceId} on date {Date}", resourceId, scheduleDate);

        return await _context.EquipmentSchedules
            .Include(s => s.OperatingPattern)
            .Where(s => s.ResourceId == resourceId && s.ScheduleDate == scheduleDate)
            .OrderBy(s => s.PlannedStartTime)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<EquipmentSchedule>> GetByResourceAndDateRangeAsync(long resourceId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var startDateOnly = startDate.Date;
        var endDateOnly = endDate.Date;

        if (endDateOnly < startDateOnly)
        {
            _logger.LogWarning("Invalid date range for schedules: {StartDate} to {EndDate}",
                startDateOnly, endDateOnly);
            throw new ArgumentException("End date cannot be before start date", nameof(endDate));
        }

        _logger.LogDebug("Getting equipment schedules for resource {ResourceId} from {StartDate} to {EndDate}",
            resourceId, startDateOnly, endDateOnly);

        try
        {
            var schedules = await _context.EquipmentSchedules
                .Include(s => s.OperatingPattern)
                .Where(s => s.ResourceId == resourceId)
                .Where(s => s.ScheduleDate >= startDateOnly && s.ScheduleDate <= endDateOnly)
                .OrderBy(s => s.ScheduleDate)
                .ThenBy(s => s.PlannedStartTime)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully retrieved {ScheduleCount} schedules for resource {ResourceId} in date range {StartDate} to {EndDate}",
                schedules.Count, resourceId, startDateOnly, endDateOnly);

            return schedules;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get schedules for resource {ResourceId} from {StartDate} to {EndDate}",
                resourceId, startDateOnly, endDateOnly);
            throw;
        }
    }

    public async Task<IEnumerable<EquipmentSchedule>> GetByStatusAsync(ScheduleStatus status, long? resourceId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting equipment schedules by status {Status}, resourceId: {ResourceId}", status, resourceId);

        var query = _context.EquipmentSchedules
            .Include(s => s.Resource)
            .Include(s => s.OperatingPattern)
            .Where(s => s.Status == status);

        if (resourceId.HasValue)
        {
            query = query.Where(s => s.ResourceId == resourceId.Value);
        }

        return await query
            .OrderBy(s => s.ScheduleDate)
            .ThenBy(s => s.ResourceId)
            .ThenBy(s => s.PlannedStartTime)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<EquipmentSchedule>> GetExceptionSchedulesAsync(long resourceId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting exception schedules for resource {ResourceId}, startDate: {StartDate}, endDate: {EndDate}",
            resourceId, startDate, endDate);

        var query = _context.EquipmentSchedules
            .Include(s => s.OperatingPattern)
            .Where(s => s.ResourceId == resourceId && s.IsException);

        if (startDate.HasValue)
        {
            query = query.Where(s => s.ScheduleDate >= startDate.Value.Date);
        }

        if (endDate.HasValue)
        {
            query = query.Where(s => s.ScheduleDate <= endDate.Value.Date);
        }

        return await query
            .OrderBy(s => s.ScheduleDate)
            .ThenBy(s => s.PlannedStartTime)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<EquipmentSchedule>> GetByPatternIdAsync(int patternId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting schedules for pattern {PatternId}, startDate: {StartDate}, endDate: {EndDate}",
            patternId, startDate, endDate);

        var query = _context.EquipmentSchedules
            .Include(s => s.Resource)
            .Where(s => s.PatternId == patternId);

        if (startDate.HasValue)
        {
            query = query.Where(s => s.ScheduleDate >= startDate.Value.Date);
        }

        if (endDate.HasValue)
        {
            query = query.Where(s => s.ScheduleDate <= endDate.Value.Date);
        }

        return await query
            .OrderBy(s => s.ScheduleDate)
            .ThenBy(s => s.ResourceId)
            .ThenBy(s => s.PlannedStartTime)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<EquipmentSchedule>> GetActiveSchedulesAtTimeAsync(DateTime dateTime, long? resourceId = null, CancellationToken cancellationToken = default)
    {
        var scheduleDate = dateTime.Date;
        _logger.LogDebug("Getting active schedules at {DateTime}, resourceId: {ResourceId}", dateTime, resourceId);

        var query = _context.EquipmentSchedules
            .Include(s => s.Resource)
            .Include(s => s.OperatingPattern)
            .Where(s => s.ScheduleDate == scheduleDate)
            .Where(s => s.PlannedStartTime <= dateTime && s.PlannedEndTime >= dateTime)
            .Where(s => s.Status == ScheduleStatus.Active);

        if (resourceId.HasValue)
        {
            query = query.Where(s => s.ResourceId == resourceId.Value);
        }

        return await query
            .OrderBy(s => s.ResourceId)
            .ThenBy(s => s.PlannedStartTime)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<(long ResourceId, DateTime Date)>> GetMissingSchedulesAsync(DateTime startDate, DateTime endDate, long? resourceId = null, CancellationToken cancellationToken = default)
    {
        var startDateOnly = startDate.Date;
        var endDateOnly = endDate.Date;

        if (endDateOnly < startDateOnly)
            throw new ArgumentException("End date cannot be before start date", nameof(endDate));

        _logger.LogDebug("Getting missing schedules from {StartDate} to {EndDate}, resourceId: {ResourceId}",
            startDateOnly, endDateOnly, resourceId);

        // Get all schedulable resources
        var resourceQuery = _context.Resources
            .Where(r => r.RequiresScheduling && r.IsActive);

        if (resourceId.HasValue)
        {
            resourceQuery = resourceQuery.Where(r => r.Id == resourceId.Value);
        }

        var resources = await resourceQuery.Select(r => r.Id).ToListAsync(cancellationToken).ConfigureAwait(false);

        // Generate all date/resource combinations
        var allCombinations = new List<(long ResourceId, DateTime Date)>();
        foreach (var resId in resources)
        {
            for (var date = startDateOnly; date <= endDateOnly; date = date.AddDays(1))
            {
                allCombinations.Add((resId, date));
            }
        }

        // Get existing schedules
        var existingSchedules = await _context.EquipmentSchedules
            .Where(s => s.ScheduleDate >= startDateOnly && s.ScheduleDate <= endDateOnly)
            .Where(s => resourceId == null || s.ResourceId == resourceId.Value)
            .Select(s => new { s.ResourceId, s.ScheduleDate })
            .Distinct()
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var existingSet = existingSchedules.Select(es => (es.ResourceId, es.ScheduleDate)).ToHashSet();

        // Return missing combinations
        return allCombinations.Where(combo => !existingSet.Contains(combo));
    }

    public async Task AddAsync(EquipmentSchedule schedule, CancellationToken cancellationToken = default)
    {
        if (schedule == null)
            throw new ArgumentNullException(nameof(schedule));

        _logger.LogDebug("Adding equipment schedule for resource {ResourceId} on date {Date}",
            schedule.ResourceId, schedule.ScheduleDate);

        await _context.EquipmentSchedules.AddAsync(schedule, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Added equipment schedule {ScheduleId} for resource {ResourceId}",
            schedule.Id, schedule.ResourceId);
    }

    public async Task AddRangeAsync(IEnumerable<EquipmentSchedule> schedules, CancellationToken cancellationToken = default)
    {
        if (schedules == null)
            throw new ArgumentNullException(nameof(schedules));

        var scheduleList = schedules.ToList();
        if (!scheduleList.Any())
        {
            _logger.LogDebug("No schedules to add, skipping bulk insert");
            return;
        }

        _logger.LogDebug("Adding {Count} equipment schedules", scheduleList.Count);

        try
        {
            await _context.EquipmentSchedules.AddRangeAsync(scheduleList, cancellationToken).ConfigureAwait(false);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully added {Count} equipment schedules", scheduleList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add {Count} equipment schedules", scheduleList.Count);
            throw;
        }
    }

    public async Task UpdateAsync(EquipmentSchedule schedule, CancellationToken cancellationToken = default)
    {
        if (schedule == null)
            throw new ArgumentNullException(nameof(schedule));

        _logger.LogDebug("Updating equipment schedule {ScheduleId}", schedule.Id);

        _context.EquipmentSchedules.Update(schedule);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Updated equipment schedule {ScheduleId}", schedule.Id);
    }

    public async Task DeleteByResourceAndDateRangeAsync(long resourceId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var startDateOnly = startDate.Date;
        var endDateOnly = endDate.Date;

        if (endDateOnly < startDateOnly)
        {
            _logger.LogWarning("Invalid date range for schedule deletion: {StartDate} to {EndDate}",
                startDateOnly, endDateOnly);
            throw new ArgumentException("End date cannot be before start date", nameof(endDate));
        }

        _logger.LogDebug("Deleting equipment schedules for resource {ResourceId} from {StartDate} to {EndDate}",
            resourceId, startDateOnly, endDateOnly);

        try
        {
            var schedulesToDelete = await _context.EquipmentSchedules
                .Where(s => s.ResourceId == resourceId)
                .Where(s => s.ScheduleDate >= startDateOnly && s.ScheduleDate <= endDateOnly)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            if (schedulesToDelete.Any())
            {
                _context.EquipmentSchedules.RemoveRange(schedulesToDelete);
                await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                _logger.LogInformation("Successfully deleted {Count} equipment schedules for resource {ResourceId}",
                    schedulesToDelete.Count, resourceId);
            }
            else
            {
                _logger.LogDebug("No schedules found to delete for resource {ResourceId} in date range {StartDate} to {EndDate}",
                    resourceId, startDateOnly, endDateOnly);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete schedules for resource {ResourceId} from {StartDate} to {EndDate}",
                resourceId, startDateOnly, endDateOnly);
            throw;
        }
    }

    public async Task<IEnumerable<EquipmentSchedule>> GetConflictingSchedulesAsync(long resourceId, DateTime date, DateTime? startTime = null, DateTime? endTime = null, long? excludeId = null, CancellationToken cancellationToken = default)
    {
        var scheduleDate = date.Date;
        _logger.LogDebug("Checking for conflicting schedules for resource {ResourceId} on {Date}, excludeId: {ExcludeId}",
            resourceId, scheduleDate, excludeId);

        var query = _context.EquipmentSchedules
            .Where(s => s.ResourceId == resourceId && s.ScheduleDate == scheduleDate);

        if (excludeId.HasValue)
        {
            query = query.Where(s => s.Id != excludeId.Value);
        }

        // If specific times are provided, check for time overlap
        if (startTime.HasValue && endTime.HasValue)
        {
            query = query.Where(s =>
                (s.PlannedStartTime <= endTime.Value && s.PlannedEndTime >= startTime.Value));
        }

        return await query
            .OrderBy(s => s.PlannedStartTime)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
