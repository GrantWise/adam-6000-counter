using Industrial.Adam.EquipmentScheduling.Domain.Entities;

namespace Industrial.Adam.EquipmentScheduling.Domain.Interfaces;

/// <summary>
/// Service interface for equipment scheduling operations
/// </summary>
public interface ISchedulingService
{
    /// <summary>
    /// Generates schedules for a resource within a date range
    /// </summary>
    /// <param name="resourceId">The resource identifier</param>
    /// <param name="startDate">The start date</param>
    /// <param name="endDate">The end date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of generated schedules</returns>
    public Task<IEnumerable<EquipmentSchedule>> GenerateSchedulesAsync(long resourceId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Regenerates schedules for a resource when pattern assignments change
    /// </summary>
    /// <param name="resourceId">The resource identifier</param>
    /// <param name="startDate">The start date</param>
    /// <param name="endDate">The end date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of regenerated schedules</returns>
    public Task<IEnumerable<EquipmentSchedule>> RegenerateSchedulesAsync(long resourceId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that a pattern assignment doesn't conflict with existing assignments
    /// </summary>
    /// <param name="resourceId">The resource identifier</param>
    /// <param name="startDate">The assignment start date</param>
    /// <param name="endDate">The assignment end date</param>
    /// <param name="excludeAssignmentId">Assignment ID to exclude from validation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with any conflicts found</returns>
    public Task<ValidationResult> ValidatePatternAssignmentAsync(long resourceId, DateTime startDate, DateTime? endDate = null, long? excludeAssignmentId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the effective pattern for a resource on a specific date
    /// </summary>
    /// <param name="resourceId">The resource identifier</param>
    /// <param name="date">The date to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The effective operating pattern, or null if none assigned</returns>
    public Task<OperatingPattern?> GetEffectivePatternAsync(long resourceId, DateTime date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates availability for a resource within a date range
    /// </summary>
    /// <param name="resourceId">The resource identifier</param>
    /// <param name="startDate">The start date</param>
    /// <param name="endDate">The end date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Availability calculation results</returns>
    public Task<AvailabilityCalculation> CalculateAvailabilityAsync(long resourceId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets schedule conflicts for a resource within a date range
    /// </summary>
    /// <param name="resourceId">The resource identifier</param>
    /// <param name="startDate">The start date</param>
    /// <param name="endDate">The end date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of schedule conflicts</returns>
    public Task<IEnumerable<ScheduleConflict>> GetScheduleConflictsAsync(long resourceId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an exception schedule for maintenance or other planned downtime
    /// </summary>
    /// <param name="resourceId">The resource identifier</param>
    /// <param name="date">The exception date</param>
    /// <param name="startTime">The start time of the exception</param>
    /// <param name="endTime">The end time of the exception</param>
    /// <param name="reason">The reason for the exception</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created exception schedule</returns>
    public Task<EquipmentSchedule> CreateExceptionScheduleAsync(long resourceId, DateTime date, DateTime startTime, DateTime endTime, string reason, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the result of validating a pattern assignment
/// </summary>
public record ValidationResult(
    bool IsValid,
    IEnumerable<string> Errors,
    IEnumerable<PatternAssignment> ConflictingAssignments);

/// <summary>
/// Represents an availability calculation for a resource
/// </summary>
public record AvailabilityCalculation(
    long ResourceId,
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalPlannedHours,
    decimal TotalAvailableHours,
    decimal AvailabilityPercentage,
    IEnumerable<EquipmentSchedule> Schedules);

/// <summary>
/// Represents a scheduling conflict
/// </summary>
public record ScheduleConflict(
    long ResourceId,
    DateTime Date,
    string ConflictType,
    string Description,
    IEnumerable<EquipmentSchedule> ConflictingSchedules);
