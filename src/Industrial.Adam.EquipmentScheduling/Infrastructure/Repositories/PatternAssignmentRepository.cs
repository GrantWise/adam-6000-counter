using Industrial.Adam.EquipmentScheduling.Domain.Entities;
using Industrial.Adam.EquipmentScheduling.Domain.Interfaces;
using Industrial.Adam.EquipmentScheduling.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.EquipmentScheduling.Infrastructure.Repositories;

/// <summary>
/// Entity Framework implementation of IPatternAssignmentRepository
/// </summary>
public sealed class PatternAssignmentRepository : IPatternAssignmentRepository
{
    private readonly EquipmentSchedulingDbContext _context;
    private readonly ILogger<PatternAssignmentRepository> _logger;

    public PatternAssignmentRepository(
        EquipmentSchedulingDbContext context,
        ILogger<PatternAssignmentRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PatternAssignment?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting pattern assignment by ID {AssignmentId}", id);

        return await _context.PatternAssignments
            .Include(pa => pa.Resource)
            .Include(pa => pa.OperatingPattern)
            .FirstOrDefaultAsync(pa => pa.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<PatternAssignment>> GetByResourceIdAsync(long resourceId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting pattern assignments for resource {ResourceId}", resourceId);

        return await _context.PatternAssignments
            .Include(pa => pa.OperatingPattern)
            .Where(pa => pa.ResourceId == resourceId)
            .OrderBy(pa => pa.EffectiveDate)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<PatternAssignment?> GetActiveAssignmentAsync(long resourceId, DateTime date, CancellationToken cancellationToken = default)
    {
        var checkDate = date.Date;
        _logger.LogDebug("Getting active pattern assignment for resource {ResourceId} on date {Date}", resourceId, checkDate);

        try
        {
            var assignment = await _context.PatternAssignments
                .Include(pa => pa.OperatingPattern)
                .Where(pa => pa.ResourceId == resourceId)
                .Where(pa => pa.EffectiveDate <= checkDate)
                .Where(pa => pa.EndDate == null || pa.EndDate >= checkDate)
                .OrderByDescending(pa => pa.IsOverride)  // Overrides take precedence
                .ThenByDescending(pa => pa.EffectiveDate) // Most recent effective date
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            if (assignment != null)
            {
                _logger.LogDebug("Found active assignment {AssignmentId} for resource {ResourceId} on {Date}",
                    assignment.Id, resourceId, checkDate);
            }
            else
            {
                _logger.LogDebug("No active assignment found for resource {ResourceId} on {Date}", resourceId, checkDate);
            }

            return assignment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active assignment for resource {ResourceId} on {Date}", resourceId, checkDate);
            throw;
        }
    }

    public async Task<IEnumerable<PatternAssignment>> GetByPatternIdAsync(int patternId, bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting pattern assignments for pattern {PatternId}, activeOnly: {ActiveOnly}", patternId, activeOnly);

        var query = _context.PatternAssignments
            .Include(pa => pa.Resource)
            .Where(pa => pa.PatternId == patternId);

        if (activeOnly)
        {
            var today = DateTime.UtcNow.Date;
            query = query.Where(pa => pa.EffectiveDate <= today && (pa.EndDate == null || pa.EndDate >= today));
        }

        return await query
            .OrderBy(pa => pa.ResourceId)
            .ThenBy(pa => pa.EffectiveDate)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<PatternAssignment>> GetByDateRangeAsync(long resourceId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var startDateOnly = startDate.Date;
        var endDateOnly = endDate.Date;

        if (endDateOnly < startDateOnly)
        {
            _logger.LogWarning("Invalid date range for pattern assignments: {StartDate} to {EndDate}",
                startDateOnly, endDateOnly);
            throw new ArgumentException("End date cannot be before start date", nameof(endDate));
        }

        _logger.LogDebug("Getting pattern assignments for resource {ResourceId} from {StartDate} to {EndDate}",
            resourceId, startDateOnly, endDateOnly);

        try
        {
            var assignments = await _context.PatternAssignments
                .Include(pa => pa.OperatingPattern)
                .Where(pa => pa.ResourceId == resourceId)
                .Where(pa => pa.EffectiveDate <= endDateOnly)
                .Where(pa => pa.EndDate == null || pa.EndDate >= startDateOnly)
                .OrderBy(pa => pa.EffectiveDate)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully retrieved {AssignmentCount} pattern assignments for resource {ResourceId} in date range {StartDate} to {EndDate}",
                assignments.Count, resourceId, startDateOnly, endDateOnly);

            return assignments;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pattern assignments for resource {ResourceId} from {StartDate} to {EndDate}",
                resourceId, startDateOnly, endDateOnly);
            throw;
        }
    }

    public async Task<IEnumerable<PatternAssignment>> GetOverrideAssignmentsAsync(long resourceId, bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting override assignments for resource {ResourceId}, activeOnly: {ActiveOnly}", resourceId, activeOnly);

        var query = _context.PatternAssignments
            .Include(pa => pa.OperatingPattern)
            .Where(pa => pa.ResourceId == resourceId && pa.IsOverride);

        if (activeOnly)
        {
            var today = DateTime.UtcNow.Date;
            query = query.Where(pa => pa.EffectiveDate <= today && (pa.EndDate == null || pa.EndDate >= today));
        }

        return await query
            .OrderBy(pa => pa.EffectiveDate)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task AddAsync(PatternAssignment assignment, CancellationToken cancellationToken = default)
    {
        if (assignment == null)
            throw new ArgumentNullException(nameof(assignment));

        _logger.LogDebug("Adding pattern assignment for resource {ResourceId} with pattern {PatternId}",
            assignment.ResourceId, assignment.PatternId);

        try
        {
            await _context.PatternAssignments.AddAsync(assignment, cancellationToken).ConfigureAwait(false);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully added pattern assignment {AssignmentId} for resource {ResourceId}",
                assignment.Id, assignment.ResourceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add pattern assignment for resource {ResourceId} with pattern {PatternId}",
                assignment.ResourceId, assignment.PatternId);
            throw;
        }
    }

    public async Task UpdateAsync(PatternAssignment assignment, CancellationToken cancellationToken = default)
    {
        if (assignment == null)
            throw new ArgumentNullException(nameof(assignment));

        _logger.LogDebug("Updating pattern assignment {AssignmentId}", assignment.Id);

        _context.PatternAssignments.Update(assignment);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Updated pattern assignment {AssignmentId}", assignment.Id);
    }

    public async Task<IEnumerable<PatternAssignment>> GetConflictingAssignmentsAsync(long resourceId, DateTime startDate, DateTime? endDate = null, long? excludeId = null, CancellationToken cancellationToken = default)
    {
        var startDateOnly = startDate.Date;
        var endDateOnly = endDate?.Date ?? DateTime.MaxValue.Date;

        _logger.LogDebug("Checking for conflicting assignments for resource {ResourceId} from {StartDate} to {EndDate}, excludeId: {ExcludeId}",
            resourceId, startDateOnly, endDateOnly, excludeId);

        var query = _context.PatternAssignments
            .Where(pa => pa.ResourceId == resourceId)
            .Where(pa => pa.EffectiveDate <= endDateOnly)
            .Where(pa => pa.EndDate == null || pa.EndDate >= startDateOnly);

        if (excludeId.HasValue)
        {
            query = query.Where(pa => pa.Id != excludeId.Value);
        }

        return await query
            .OrderBy(pa => pa.EffectiveDate)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<PatternAssignment>> GetExpiringAssignmentsAsync(int days, CancellationToken cancellationToken = default)
    {
        if (days < 0)
            throw new ArgumentException("Days must be non-negative", nameof(days));

        var cutoffDate = DateTime.UtcNow.Date.AddDays(days);
        var today = DateTime.UtcNow.Date;

        _logger.LogDebug("Getting assignments expiring within {Days} days (by {CutoffDate})", days, cutoffDate);

        return await _context.PatternAssignments
            .Include(pa => pa.Resource)
            .Include(pa => pa.OperatingPattern)
            .Where(pa => pa.EndDate != null)
            .Where(pa => pa.EndDate >= today && pa.EndDate <= cutoffDate)
            .OrderBy(pa => pa.EndDate)
            .ThenBy(pa => pa.ResourceId)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
