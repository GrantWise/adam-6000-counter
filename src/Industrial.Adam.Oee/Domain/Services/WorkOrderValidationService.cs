using Industrial.Adam.Oee.Domain.Entities;
using Industrial.Adam.Oee.Domain.Enums;
using Industrial.Adam.Oee.Domain.Exceptions;
using Industrial.Adam.Oee.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Domain.Services;

/// <summary>
/// Implementation of work order validation service
/// </summary>
public sealed class WorkOrderValidationService : IWorkOrderValidationService
{
    private readonly IWorkOrderRepository _workOrderRepository;
    private readonly ILogger<WorkOrderValidationService> _logger;

    /// <summary>
    /// Initialize work order validation service
    /// </summary>
    /// <param name="workOrderRepository">Work order repository</param>
    /// <param name="logger">Logger instance</param>
    public WorkOrderValidationService(
        IWorkOrderRepository workOrderRepository,
        ILogger<WorkOrderValidationService> logger)
    {
        _workOrderRepository = workOrderRepository ?? throw new ArgumentNullException(nameof(workOrderRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<WorkOrderValidationResult> ValidateForCreationAsync(
        WorkOrder workOrder,
        CancellationToken cancellationToken = default)
    {
        if (workOrder == null)
            throw new ArgumentNullException(nameof(workOrder));

        _logger.LogDebug("Validating work order {WorkOrderId} for creation", workOrder.Id);

        var errors = new List<string>();
        var warnings = new List<string>();

        // Basic validation
        if (string.IsNullOrWhiteSpace(workOrder.Id))
            errors.Add("Work order ID is required");

        if (string.IsNullOrWhiteSpace(workOrder.ResourceReference))
            errors.Add("Resource reference is required");

        if (workOrder.PlannedQuantity <= 0)
            errors.Add("Target quantity must be positive");

        // Schedule validation
        if (workOrder.ScheduledStartTime >= workOrder.ScheduledEndTime)
            errors.Add("Scheduled end time must be after start time");

        // Check for duplicates
        var existingWorkOrder = await _workOrderRepository.GetByIdAsync(workOrder.Id, cancellationToken);
        if (existingWorkOrder != null)
            errors.Add($"Work order with ID '{workOrder.Id}' already exists");

        // Scheduling validation
        var schedulingResult = await ValidateSchedulingConstraintsAsync(workOrder, cancellationToken);
        if (!schedulingResult.IsValidSchedule)
        {
            errors.AddRange(schedulingResult.ScheduleIssues);
        }

        var isValid = !errors.Any();
        var validationCode = isValid ? "VALID_FOR_CREATION" : "INVALID_FOR_CREATION";

        return new WorkOrderValidationResult(isValid, errors, warnings, validationCode);
    }

    /// <inheritdoc />
    public async Task<WorkOrderValidationResult> ValidateForStartAsync(
        string workOrderId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workOrderId))
            throw new ArgumentException("Work order ID cannot be null or empty", nameof(workOrderId));

        _logger.LogDebug("Validating work order {WorkOrderId} for start", workOrderId);

        var errors = new List<string>();
        var warnings = new List<string>();

        var workOrder = await _workOrderRepository.GetByIdAsync(workOrderId, cancellationToken);
        if (workOrder == null)
        {
            errors.Add($"Work order '{workOrderId}' not found");
            return new WorkOrderValidationResult(false, errors, warnings, "WORK_ORDER_NOT_FOUND");
        }

        // Check current status
        if (workOrder.ActualStartTime.HasValue)
            errors.Add("Work order has already been started");

        // Check scheduling
        if (DateTime.UtcNow < workOrder.ScheduledStartTime.AddHours(-1)) // Allow 1 hour early start
            warnings.Add("Starting work order earlier than scheduled time");

        // Resource availability
        var resourceResult = await ValidateResourceAvailabilityAsync(
            workOrderId, workOrder.ResourceReference, cancellationToken);

        if (!resourceResult.IsAvailable)
        {
            errors.Add($"Resource '{workOrder.ResourceReference}' is not available");
            errors.AddRange(resourceResult.ConflictingWorkOrders.Select(wo =>
                $"Resource conflict with work order: {wo}"));
        }

        var isValid = !errors.Any();
        var validationCode = isValid ? "VALID_FOR_START" : "INVALID_FOR_START";

        return new WorkOrderValidationResult(isValid, errors, warnings, validationCode);
    }

    /// <inheritdoc />
    public async Task<WorkOrderValidationResult> ValidateForCompletionAsync(
        string workOrderId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workOrderId))
            throw new ArgumentException("Work order ID cannot be null or empty", nameof(workOrderId));

        _logger.LogDebug("Validating work order {WorkOrderId} for completion", workOrderId);

        var errors = new List<string>();
        var warnings = new List<string>();

        var workOrder = await _workOrderRepository.GetByIdAsync(workOrderId, cancellationToken);
        if (workOrder == null)
        {
            errors.Add($"Work order '{workOrderId}' not found");
            return new WorkOrderValidationResult(false, errors, warnings, "WORK_ORDER_NOT_FOUND");
        }

        // Check current status
        if (!workOrder.ActualStartTime.HasValue)
            errors.Add("Work order must be started before it can be completed");

        if (workOrder.ActualEndTime.HasValue)
            errors.Add("Work order has already been completed");

        // Check production quantities
        if (workOrder.TotalQuantityProduced == 0)
            warnings.Add("No production recorded for this work order");

        var completionPercentage = workOrder.PlannedQuantity > 0
            ? (workOrder.TotalQuantityProduced / workOrder.PlannedQuantity) * 100
            : 0;

        if (completionPercentage < 50)
            warnings.Add($"Work order is only {completionPercentage:F1}% complete");

        var isValid = !errors.Any();
        var validationCode = isValid ? "VALID_FOR_COMPLETION" : "INVALID_FOR_COMPLETION";

        return new WorkOrderValidationResult(isValid, errors, warnings, validationCode);
    }

    /// <inheritdoc />
    public async Task<ResourceValidationResult> ValidateResourceAvailabilityAsync(
        string workOrderId,
        string resourceReference,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workOrderId))
            throw new ArgumentException("Work order ID cannot be null or empty", nameof(workOrderId));

        if (string.IsNullOrWhiteSpace(resourceReference))
            throw new ArgumentException("Resource reference cannot be null or empty", nameof(resourceReference));

        _logger.LogDebug(
            "Validating resource availability for work order {WorkOrderId}, resource {ResourceReference}",
            workOrderId, resourceReference);

        try
        {
            var workOrder = await _workOrderRepository.GetByIdAsync(workOrderId, cancellationToken);
            if (workOrder == null)
            {
                return new ResourceValidationResult(
                    false, resourceReference, new[] { "Work order not found" }, null, null);
            }

            // Check for conflicting work orders on the same resource
            var activeWorkOrder = await _workOrderRepository.GetActiveByDeviceAsync(
                resourceReference, cancellationToken);

            var conflicts = new List<string>();
            if (activeWorkOrder != null &&
                activeWorkOrder.Id != workOrderId &&
                activeWorkOrder.ActualStartTime.HasValue &&
                !activeWorkOrder.ActualEndTime.HasValue)
            {
                conflicts.Add(activeWorkOrder.Id);
            }

            var isAvailable = !conflicts.Any();

            // Simple availability window (could be enhanced with actual scheduling)
            var availableFrom = isAvailable ? DateTime.UtcNow : (DateTime?)null;
            var availableUntil = isAvailable ? DateTime.UtcNow.AddDays(1) : (DateTime?)null;

            return new ResourceValidationResult(
                isAvailable,
                resourceReference,
                conflicts,
                availableFrom,
                availableUntil
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error validating resource availability for work order {WorkOrderId}, resource {ResourceReference}",
                workOrderId, resourceReference);

            return new ResourceValidationResult(
                false, resourceReference, new[] { "Error checking resource availability" }, null, null);
        }
    }

    /// <inheritdoc />
    public async Task<SchedulingValidationResult> ValidateSchedulingConstraintsAsync(
        WorkOrder workOrder,
        CancellationToken cancellationToken = default)
    {
        if (workOrder == null)
            throw new ArgumentNullException(nameof(workOrder));

        _logger.LogDebug("Validating scheduling constraints for work order {WorkOrderId}", workOrder.Id);

        await Task.CompletedTask; // Placeholder for async operations

        var issues = new List<string>();

        // Basic time validation
        if (workOrder.ScheduledStartTime >= workOrder.ScheduledEndTime)
            issues.Add("Scheduled end time must be after start time");

        // Check if scheduled in the past
        if (workOrder.ScheduledStartTime < DateTime.UtcNow.AddMinutes(-30)) // Allow 30 min grace period
            issues.Add("Work order is scheduled in the past");

        // Check reasonable duration
        var scheduledDuration = workOrder.ScheduledEndTime - workOrder.ScheduledStartTime;
        if (scheduledDuration.TotalMinutes < 1)
            issues.Add("Scheduled duration is too short (minimum 1 minute)");

        if (scheduledDuration.TotalHours > 168) // 1 week
            issues.Add("Scheduled duration is unusually long (over 1 week)");

        // Estimate if target quantity is realistic for time period
        if (workOrder.PlannedQuantity > 0 && scheduledDuration.TotalMinutes > 0)
        {
            var requiredRate = workOrder.PlannedQuantity / (decimal)scheduledDuration.TotalMinutes;
            if (requiredRate > 100) // Arbitrary high rate threshold
                issues.Add($"Required production rate ({requiredRate:F1} units/min) may be unrealistic");
        }

        var isValid = !issues.Any();

        // Provide recommendations
        DateTime? recommendedStart = null;
        DateTime? recommendedEnd = null;

        if (!isValid)
        {
            recommendedStart = DateTime.UtcNow.AddMinutes(30); // Start in 30 minutes
            recommendedEnd = recommendedStart.Value.AddHours(2); // 2-hour duration
        }

        return new SchedulingValidationResult(
            isValid,
            issues,
            recommendedStart,
            recommendedEnd
        );
    }
}
