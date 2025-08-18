using Industrial.Adam.EquipmentScheduling.Domain.Entities;

namespace Industrial.Adam.EquipmentScheduling.Domain.Interfaces;

/// <summary>
/// Repository interface for pattern assignment operations
/// </summary>
public interface IPatternAssignmentRepository
{
    /// <summary>
    /// Gets a pattern assignment by its identifier
    /// </summary>
    /// <param name="id">The assignment identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The pattern assignment, or null if not found</returns>
    public Task<PatternAssignment?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all pattern assignments for a resource
    /// </summary>
    /// <param name="resourceId">The resource identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of pattern assignments</returns>
    public Task<IEnumerable<PatternAssignment>> GetByResourceIdAsync(long resourceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the active pattern assignment for a resource on a specific date
    /// </summary>
    /// <param name="resourceId">The resource identifier</param>
    /// <param name="date">The date to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The active pattern assignment, or null if none found</returns>
    public Task<PatternAssignment?> GetActiveAssignmentAsync(long resourceId, DateTime date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all assignments for a specific pattern
    /// </summary>
    /// <param name="patternId">The pattern identifier</param>
    /// <param name="activeOnly">Whether to include only active assignments</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of pattern assignments</returns>
    public Task<IEnumerable<PatternAssignment>> GetByPatternIdAsync(int patternId, bool activeOnly = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets assignments within a date range for a resource
    /// </summary>
    /// <param name="resourceId">The resource identifier</param>
    /// <param name="startDate">The start date of the range</param>
    /// <param name="endDate">The end date of the range</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of pattern assignments in the date range</returns>
    public Task<IEnumerable<PatternAssignment>> GetByDateRangeAsync(long resourceId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets override assignments for a resource
    /// </summary>
    /// <param name="resourceId">The resource identifier</param>
    /// <param name="activeOnly">Whether to include only active overrides</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of override pattern assignments</returns>
    public Task<IEnumerable<PatternAssignment>> GetOverrideAssignmentsAsync(long resourceId, bool activeOnly = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new pattern assignment
    /// </summary>
    /// <param name="assignment">The assignment to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task AddAsync(PatternAssignment assignment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing pattern assignment
    /// </summary>
    /// <param name="assignment">The assignment to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task UpdateAsync(PatternAssignment assignment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks for conflicting assignments
    /// </summary>
    /// <param name="resourceId">The resource identifier</param>
    /// <param name="startDate">The start date to check</param>
    /// <param name="endDate">The end date to check (null for indefinite)</param>
    /// <param name="excludeId">Assignment ID to exclude from the check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of conflicting assignments</returns>
    public Task<IEnumerable<PatternAssignment>> GetConflictingAssignmentsAsync(long resourceId, DateTime startDate, DateTime? endDate = null, long? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets assignments that expire within a specified number of days
    /// </summary>
    /// <param name="days">Number of days to look ahead</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of assignments expiring soon</returns>
    public Task<IEnumerable<PatternAssignment>> GetExpiringAssignmentsAsync(int days, CancellationToken cancellationToken = default);
}
