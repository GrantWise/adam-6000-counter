using Industrial.Adam.EquipmentScheduling.Domain.Entities;
using Industrial.Adam.EquipmentScheduling.Domain.Enums;

namespace Industrial.Adam.EquipmentScheduling.Domain.Interfaces;

/// <summary>
/// Repository interface for equipment schedule operations
/// </summary>
public interface IEquipmentScheduleRepository
{
    /// <summary>
    /// Gets an equipment schedule by its identifier
    /// </summary>
    /// <param name="id">The schedule identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The equipment schedule, or null if not found</returns>
    public Task<EquipmentSchedule?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets schedules for a resource on a specific date
    /// </summary>
    /// <param name="resourceId">The resource identifier</param>
    /// <param name="date">The schedule date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of schedules for the date</returns>
    public Task<IEnumerable<EquipmentSchedule>> GetByResourceAndDateAsync(long resourceId, DateTime date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets schedules for a resource within a date range
    /// </summary>
    /// <param name="resourceId">The resource identifier</param>
    /// <param name="startDate">The start date of the range</param>
    /// <param name="endDate">The end date of the range</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of schedules in the date range</returns>
    public Task<IEnumerable<EquipmentSchedule>> GetByResourceAndDateRangeAsync(long resourceId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets schedules by status
    /// </summary>
    /// <param name="status">The schedule status</param>
    /// <param name="resourceId">Optional resource identifier to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of schedules with the specified status</returns>
    public Task<IEnumerable<EquipmentSchedule>> GetByStatusAsync(ScheduleStatus status, long? resourceId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets exception schedules for a resource
    /// </summary>
    /// <param name="resourceId">The resource identifier</param>
    /// <param name="startDate">Optional start date filter</param>
    /// <param name="endDate">Optional end date filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of exception schedules</returns>
    public Task<IEnumerable<EquipmentSchedule>> GetExceptionSchedulesAsync(long resourceId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets schedules generated from a specific pattern
    /// </summary>
    /// <param name="patternId">The pattern identifier</param>
    /// <param name="startDate">Optional start date filter</param>
    /// <param name="endDate">Optional end date filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of schedules generated from the pattern</returns>
    public Task<IEnumerable<EquipmentSchedule>> GetByPatternIdAsync(int patternId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active schedules at a specific time
    /// </summary>
    /// <param name="dateTime">The date and time to check</param>
    /// <param name="resourceId">Optional resource identifier to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active schedules at the specified time</returns>
    public Task<IEnumerable<EquipmentSchedule>> GetActiveSchedulesAtTimeAsync(DateTime dateTime, long? resourceId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets schedules that need to be generated for a date range
    /// </summary>
    /// <param name="startDate">The start date</param>
    /// <param name="endDate">The end date</param>
    /// <param name="resourceId">Optional resource identifier to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of resources and dates that need schedule generation</returns>
    public Task<IEnumerable<(long ResourceId, DateTime Date)>> GetMissingSchedulesAsync(DateTime startDate, DateTime endDate, long? resourceId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new equipment schedule
    /// </summary>
    /// <param name="schedule">The schedule to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task AddAsync(EquipmentSchedule schedule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple equipment schedules in a batch
    /// </summary>
    /// <param name="schedules">The schedules to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task AddRangeAsync(IEnumerable<EquipmentSchedule> schedules, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing equipment schedule
    /// </summary>
    /// <param name="schedule">The schedule to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task UpdateAsync(EquipmentSchedule schedule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes equipment schedules for a resource and date range
    /// </summary>
    /// <param name="resourceId">The resource identifier</param>
    /// <param name="startDate">The start date</param>
    /// <param name="endDate">The end date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task DeleteByResourceAndDateRangeAsync(long resourceId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks for conflicting schedules
    /// </summary>
    /// <param name="resourceId">The resource identifier</param>
    /// <param name="date">The schedule date</param>
    /// <param name="startTime">The start time</param>
    /// <param name="endTime">The end time</param>
    /// <param name="excludeId">Schedule ID to exclude from the check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of conflicting schedules</returns>
    public Task<IEnumerable<EquipmentSchedule>> GetConflictingSchedulesAsync(long resourceId, DateTime date, DateTime? startTime = null, DateTime? endTime = null, long? excludeId = null, CancellationToken cancellationToken = default);
}
