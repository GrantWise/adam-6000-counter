using Industrial.Adam.Oee.Domain.Entities;
using Industrial.Adam.Oee.Domain.Enums;
using Industrial.Adam.Oee.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Domain.Services;

/// <summary>
/// Domain service for work order validation and business rule enforcement
/// Extracts validation logic from WorkOrder entity to follow SRP
/// </summary>
public interface IWorkOrderValidationService
{
    /// <summary>
    /// Validate work order creation parameters
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="plannedQuantity">Planned quantity</param>
    /// <param name="scheduledStartTime">Scheduled start time</param>
    /// <param name="scheduledEndTime">Scheduled end time</param>
    /// <param name="resourceReference">Resource reference</param>
    /// <returns>Validation result</returns>
    public ValidationResult ValidateCreation(
        string workOrderId,
        decimal plannedQuantity,
        DateTime scheduledStartTime,
        DateTime scheduledEndTime,
        string resourceReference);

    /// <summary>
    /// Validate state transition
    /// </summary>
    /// <param name="currentStatus">Current work order status</param>
    /// <param name="targetStatus">Target status</param>
    /// <returns>Validation result</returns>
    public ValidationResult ValidateStateTransition(WorkOrderStatus currentStatus, WorkOrderStatus targetStatus);

    /// <summary>
    /// Validate counter data update
    /// </summary>
    /// <param name="goodCount">Good pieces count</param>
    /// <param name="scrapCount">Scrap pieces count</param>
    /// <returns>Validation result</returns>
    public ValidationResult ValidateCounterDataUpdate(decimal goodCount, decimal scrapCount);

    /// <summary>
    /// Validate work order business rules
    /// </summary>
    /// <param name="workOrder">Work order to validate</param>
    /// <returns>Validation result</returns>
    public ValidationResult ValidateBusinessRules(WorkOrder workOrder);

    /// <summary>
    /// Check if work order can be started
    /// </summary>
    /// <param name="workOrder">Work order</param>
    /// <returns>True if can be started</returns>
    public bool CanStart(WorkOrder workOrder);

    /// <summary>
    /// Check if work order can be paused
    /// </summary>
    /// <param name="workOrder">Work order</param>
    /// <returns>True if can be paused</returns>
    public bool CanPause(WorkOrder workOrder);

    /// <summary>
    /// Check if work order can be resumed
    /// </summary>
    /// <param name="workOrder">Work order</param>
    /// <returns>True if can be resumed</returns>
    public bool CanResume(WorkOrder workOrder);

    /// <summary>
    /// Check if work order can be completed
    /// </summary>
    /// <param name="workOrder">Work order</param>
    /// <returns>True if can be completed</returns>
    public bool CanComplete(WorkOrder workOrder);

    /// <summary>
    /// Check if work order can be cancelled
    /// </summary>
    /// <param name="workOrder">Work order</param>
    /// <returns>True if can be cancelled</returns>
    public bool CanCancel(WorkOrder workOrder);
}

/// <summary>
/// Implementation of work order validation service
/// </summary>
public sealed class WorkOrderValidationService : IWorkOrderValidationService
{
    private readonly ILogger<WorkOrderValidationService> _logger;

    /// <summary>
    /// Initialize work order validation service
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public WorkOrderValidationService(ILogger<WorkOrderValidationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public ValidationResult ValidateCreation(
        string workOrderId,
        decimal plannedQuantity,
        DateTime scheduledStartTime,
        DateTime scheduledEndTime,
        string resourceReference)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(workOrderId))
        {
            errors.Add("Work order ID is required");
        }

        if (plannedQuantity <= 0)
        {
            errors.Add("Planned quantity must be positive");
        }

        if (scheduledEndTime <= scheduledStartTime)
        {
            errors.Add("Scheduled end time must be after start time");
        }

        if (string.IsNullOrWhiteSpace(resourceReference))
        {
            errors.Add("Resource reference is required");
        }

        // Business rule: Work orders should not be scheduled more than 1 year in advance
        if (scheduledStartTime > DateTime.UtcNow.AddYears(1))
        {
            errors.Add("Work orders cannot be scheduled more than 1 year in advance");
        }

        // Business rule: Work orders should not be scheduled in the past (with 5-minute grace period)
        if (scheduledStartTime < DateTime.UtcNow.AddMinutes(-5))
        {
            errors.Add("Work orders cannot be scheduled in the past");
        }

        // Business rule: Reasonable duration limits
        var duration = scheduledEndTime - scheduledStartTime;
        if (duration.TotalMinutes < 1)
        {
            errors.Add("Work order duration must be at least 1 minute");
        }

        if (duration.TotalDays > 30)
        {
            errors.Add("Work order duration cannot exceed 30 days");
        }

        var result = new ValidationResult(errors.Count == 0, errors);

        if (!result.IsValid)
        {
            _logger.LogWarning(
                "Work order creation validation failed for {WorkOrderId}: {Errors}",
                workOrderId, string.Join(", ", errors));
        }

        return result;
    }

    /// <inheritdoc />
    public ValidationResult ValidateStateTransition(WorkOrderStatus currentStatus, WorkOrderStatus targetStatus)
    {
        var errors = new List<string>();

        // Define valid state transitions
        var validTransitions = new Dictionary<WorkOrderStatus, HashSet<WorkOrderStatus>>
        {
            { WorkOrderStatus.Pending, new HashSet<WorkOrderStatus> { WorkOrderStatus.Active, WorkOrderStatus.Cancelled } },
            { WorkOrderStatus.Active, new HashSet<WorkOrderStatus> { WorkOrderStatus.Paused, WorkOrderStatus.Completed, WorkOrderStatus.Cancelled } },
            { WorkOrderStatus.Paused, new HashSet<WorkOrderStatus> { WorkOrderStatus.Active, WorkOrderStatus.Cancelled } },
            { WorkOrderStatus.Completed, new HashSet<WorkOrderStatus>() }, // Terminal state
            { WorkOrderStatus.Cancelled, new HashSet<WorkOrderStatus>() }  // Terminal state
        };

        if (!validTransitions.TryGetValue(currentStatus, out var allowedTargets) ||
            !allowedTargets.Contains(targetStatus))
        {
            errors.Add($"Invalid state transition from {currentStatus} to {targetStatus}");
        }

        var result = new ValidationResult(errors.Count == 0, errors);

        if (!result.IsValid)
        {
            _logger.LogWarning(
                "State transition validation failed: {CurrentStatus} -> {TargetStatus}",
                currentStatus, targetStatus);
        }

        return result;
    }

    /// <inheritdoc />
    public ValidationResult ValidateCounterDataUpdate(decimal goodCount, decimal scrapCount)
    {
        var errors = new List<string>();

        if (goodCount < 0)
        {
            errors.Add("Good count cannot be negative");
        }

        if (scrapCount < 0)
        {
            errors.Add("Scrap count cannot be negative");
        }

        // Business rule: Reasonable production limits
        if (goodCount > 1_000_000)
        {
            errors.Add("Good count exceeds reasonable production limits");
        }

        if (scrapCount > 100_000)
        {
            errors.Add("Scrap count exceeds reasonable limits");
        }

        // Business rule: Quality check
        var totalProduced = goodCount + scrapCount;
        if (totalProduced > 0)
        {
            var yieldPercentage = (goodCount / totalProduced) * 100;
            if (yieldPercentage < 1) // Less than 1% yield is suspicious
            {
                errors.Add($"Extremely low yield detected: {yieldPercentage:F1}%");
            }
        }

        var result = new ValidationResult(errors.Count == 0, errors);

        if (!result.IsValid)
        {
            _logger.LogWarning(
                "Counter data validation failed: Good={Good}, Scrap={Scrap}, Errors={Errors}",
                goodCount, scrapCount, string.Join(", ", errors));
        }

        return result;
    }

    /// <inheritdoc />
    public ValidationResult ValidateBusinessRules(WorkOrder workOrder)
    {
        if (workOrder == null)
            throw new ArgumentNullException(nameof(workOrder));

        var errors = new List<string>();

        // Rule: Active work orders should have actual start time
        if (workOrder.Status == WorkOrderStatus.Active && workOrder.ActualStartTime == null)
        {
            errors.Add("Active work orders must have an actual start time");
        }

        // Rule: Completed work orders should have actual end time
        if (workOrder.Status == WorkOrderStatus.Completed && workOrder.ActualEndTime == null)
        {
            errors.Add("Completed work orders must have an actual end time");
        }

        // Rule: Actual times should be logical
        if (workOrder.ActualStartTime.HasValue && workOrder.ActualEndTime.HasValue)
        {
            if (workOrder.ActualEndTime <= workOrder.ActualStartTime)
            {
                errors.Add("Actual end time must be after actual start time");
            }
        }

        // Rule: Production quantities should be reasonable
        if (workOrder.TotalQuantityProduced > workOrder.PlannedQuantity * 2)
        {
            errors.Add($"Production ({workOrder.TotalQuantityProduced}) significantly exceeds plan ({workOrder.PlannedQuantity})");
        }

        // Rule: Check for suspiciously long-running work orders
        if (workOrder.ActualStartTime.HasValue && workOrder.Status == WorkOrderStatus.Active)
        {
            var runningTime = DateTime.UtcNow - workOrder.ActualStartTime.Value;
            var scheduledDuration = workOrder.ScheduledEndTime - workOrder.ScheduledStartTime;

            if (runningTime > scheduledDuration.Add(TimeSpan.FromHours(24)))
            {
                errors.Add($"Work order has been running for {runningTime.TotalHours:F1} hours, significantly longer than scheduled");
            }
        }

        var result = new ValidationResult(errors.Count == 0, errors);

        if (!result.IsValid)
        {
            _logger.LogWarning(
                "Business rule validation failed for work order {WorkOrderId}: {Errors}",
                workOrder.Id, string.Join(", ", errors));
        }

        return result;
    }

    /// <inheritdoc />
    public bool CanStart(WorkOrder workOrder)
    {
        if (workOrder == null)
            return false;
        return workOrder.Status == WorkOrderStatus.Pending;
    }

    /// <inheritdoc />
    public bool CanPause(WorkOrder workOrder)
    {
        if (workOrder == null)
            return false;
        return workOrder.Status == WorkOrderStatus.Active;
    }

    /// <inheritdoc />
    public bool CanResume(WorkOrder workOrder)
    {
        if (workOrder == null)
            return false;
        return workOrder.Status == WorkOrderStatus.Paused;
    }

    /// <inheritdoc />
    public bool CanComplete(WorkOrder workOrder)
    {
        if (workOrder == null)
            return false;
        return workOrder.Status == WorkOrderStatus.Active || workOrder.Status == WorkOrderStatus.Paused;
    }

    /// <inheritdoc />
    public bool CanCancel(WorkOrder workOrder)
    {
        if (workOrder == null)
            return false;
        return workOrder.Status != WorkOrderStatus.Completed && workOrder.Status != WorkOrderStatus.Cancelled;
    }
}

/// <summary>
/// Validation result containing success status and error messages
/// </summary>
public sealed record ValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    /// <summary>
    /// Create successful validation result
    /// </summary>
    /// <returns>Successful validation result</returns>
    public static ValidationResult Success() => new(true, Array.Empty<string>());

    /// <summary>
    /// Create failed validation result with errors
    /// </summary>
    /// <param name="errors">Validation errors</param>
    /// <returns>Failed validation result</returns>
    public static ValidationResult Failure(params string[] errors) => new(false, errors);

    /// <summary>
    /// Create failed validation result with error list
    /// </summary>
    /// <param name="errors">Validation errors</param>
    /// <returns>Failed validation result</returns>
    public static ValidationResult Failure(IEnumerable<string> errors) => new(false, errors.ToList());

    /// <summary>
    /// Throw exception if validation failed
    /// </summary>
    /// <param name="workOrderId">Work order ID for context</param>
    /// <exception cref="WorkOrderException">Thrown if validation failed</exception>
    public void ThrowIfInvalid(string workOrderId)
    {
        if (!IsValid)
        {
            var errorMessage = $"Work order validation failed: {string.Join(", ", Errors)}";
            throw new WorkOrderException(
                errorMessage,
                workOrderId,
                OeeErrorCode.WorkOrderValidationFailed);
        }
    }
}
