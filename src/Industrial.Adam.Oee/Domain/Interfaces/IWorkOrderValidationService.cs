using Industrial.Adam.Oee.Domain.Entities;

namespace Industrial.Adam.Oee.Domain.Interfaces;

/// <summary>
/// Domain service for work order validation
/// </summary>
public interface IWorkOrderValidationService
{
    /// <summary>
    /// Validate a work order for creation
    /// </summary>
    /// <param name="workOrder">Work order to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    public Task<WorkOrderValidationResult> ValidateForCreationAsync(
        WorkOrder workOrder,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate a work order for starting
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    public Task<WorkOrderValidationResult> ValidateForStartAsync(
        string workOrderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate a work order for completion
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    public Task<WorkOrderValidationResult> ValidateForCompletionAsync(
        string workOrderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate resource availability for a work order
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="resourceReference">Resource reference</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Resource validation result</returns>
    public Task<ResourceValidationResult> ValidateResourceAvailabilityAsync(
        string workOrderId,
        string resourceReference,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate work order scheduling constraints
    /// </summary>
    /// <param name="workOrder">Work order to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Scheduling validation result</returns>
    public Task<SchedulingValidationResult> ValidateSchedulingConstraintsAsync(
        WorkOrder workOrder,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Work order validation result
/// </summary>
/// <param name="IsValid">Whether the work order is valid</param>
/// <param name="Errors">List of validation errors</param>
/// <param name="Warnings">List of validation warnings</param>
/// <param name="ValidationCode">Validation result code</param>
public record WorkOrderValidationResult(
    bool IsValid,
    IEnumerable<string> Errors,
    IEnumerable<string> Warnings,
    string ValidationCode
);

/// <summary>
/// Resource validation result
/// </summary>
/// <param name="IsAvailable">Whether the resource is available</param>
/// <param name="ResourceReference">Resource reference</param>
/// <param name="ConflictingWorkOrders">List of conflicting work orders</param>
/// <param name="AvailableFrom">When the resource becomes available</param>
/// <param name="AvailableUntil">When the resource availability ends</param>
public record ResourceValidationResult(
    bool IsAvailable,
    string ResourceReference,
    IEnumerable<string> ConflictingWorkOrders,
    DateTime? AvailableFrom,
    DateTime? AvailableUntil
);

/// <summary>
/// Scheduling validation result
/// </summary>
/// <param name="IsValidSchedule">Whether the schedule is valid</param>
/// <param name="ScheduleIssues">List of scheduling issues</param>
/// <param name="RecommendedStartTime">Recommended start time</param>
/// <param name="RecommendedEndTime">Recommended end time</param>
public record SchedulingValidationResult(
    bool IsValidSchedule,
    IEnumerable<string> ScheduleIssues,
    DateTime? RecommendedStartTime,
    DateTime? RecommendedEndTime
);
