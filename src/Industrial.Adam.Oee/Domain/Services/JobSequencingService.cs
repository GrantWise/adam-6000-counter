using Industrial.Adam.Oee.Domain.Entities;
using Industrial.Adam.Oee.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Domain.Services;

/// <summary>
/// Domain service for enforcing job sequencing business rules
/// 
/// Core Business Rule: Only one job can run on an equipment line at any given time.
/// This service validates job transitions and prevents overlapping jobs.
/// </summary>
public class JobSequencingService : IJobSequencingService
{
    private readonly IWorkOrderRepository _workOrderRepository;
    private readonly IEquipmentLineRepository _equipmentLineRepository;
    private readonly ILogger<JobSequencingService> _logger;

    /// <summary>
    /// Initializes a new instance of the JobSequencingService
    /// </summary>
    /// <param name="workOrderRepository">Repository for work order operations</param>
    /// <param name="equipmentLineRepository">Repository for equipment line operations</param>
    /// <param name="logger">Logger instance for service operations</param>
    public JobSequencingService(
        IWorkOrderRepository workOrderRepository,
        IEquipmentLineRepository equipmentLineRepository,
        ILogger<JobSequencingService> logger)
    {
        _workOrderRepository = workOrderRepository;
        _equipmentLineRepository = equipmentLineRepository;
        _logger = logger;
    }

    /// <summary>
    /// Validate that a new job can be started on the specified equipment line
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="newWorkOrderId">Work order identifier being started</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    public async Task<JobSequencingValidationResult> ValidateJobStartAsync(
        string lineId,
        string newWorkOrderId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Validating job start for work order {WorkOrderId} on line {LineId}",
            newWorkOrderId, lineId);

        try
        {
            // Validate equipment line exists and is active
            var equipmentLine = await _equipmentLineRepository.GetByLineIdAsync(lineId, cancellationToken);
            if (equipmentLine == null)
            {
                return JobSequencingValidationResult.Failure(
                    JobSequencingViolationType.EquipmentLineNotFound,
                    $"Equipment line '{lineId}' does not exist");
            }

            if (!equipmentLine.IsActive)
            {
                return JobSequencingValidationResult.Failure(
                    JobSequencingViolationType.EquipmentLineInactive,
                    $"Equipment line '{lineId}' is not active");
            }

            // Check for existing active job on the line
            var activeJob = await GetActiveJobOnLineAsync(lineId, cancellationToken);
            if (activeJob != null)
            {
                return JobSequencingValidationResult.Failure(
                    JobSequencingViolationType.OverlappingJob,
                    $"Equipment line '{lineId}' already has an active job: {activeJob.Id}",
                    activeJob);
            }

            // Check if the new work order already exists
            var existingWorkOrder = await _workOrderRepository.GetByIdAsync(newWorkOrderId, cancellationToken);
            if (existingWorkOrder != null)
            {
                return JobSequencingValidationResult.Failure(
                    JobSequencingViolationType.WorkOrderAlreadyExists,
                    $"Work order '{newWorkOrderId}' already exists");
            }

            _logger.LogDebug("Job sequencing validation passed for work order {WorkOrderId} on line {LineId}",
                newWorkOrderId, lineId);

            return JobSequencingValidationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating job start for work order {WorkOrderId} on line {LineId}",
                newWorkOrderId, lineId);

            return JobSequencingValidationResult.Failure(
                JobSequencingViolationType.ValidationError,
                $"Error validating job start: {ex.Message}");
        }
    }

    /// <summary>
    /// Validate that a job can be ended on the specified equipment line
    /// </summary>
    /// <param name="workOrderId">Work order identifier being ended</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    public async Task<JobSequencingValidationResult> ValidateJobEndAsync(
        string workOrderId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Validating job end for work order {WorkOrderId}", workOrderId);

        try
        {
            // Check if work order exists and is active
            var workOrder = await _workOrderRepository.GetByIdAsync(workOrderId, cancellationToken);
            if (workOrder == null)
            {
                return JobSequencingValidationResult.Failure(
                    JobSequencingViolationType.WorkOrderNotFound,
                    $"Work order '{workOrderId}' does not exist");
            }

            if (!workOrder.IsActive)
            {
                return JobSequencingValidationResult.Failure(
                    JobSequencingViolationType.WorkOrderNotActive,
                    $"Work order '{workOrderId}' is not active (status: {workOrder.Status})");
            }

            _logger.LogDebug("Job end validation passed for work order {WorkOrderId}", workOrderId);

            return JobSequencingValidationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating job end for work order {WorkOrderId}", workOrderId);

            return JobSequencingValidationResult.Failure(
                JobSequencingViolationType.ValidationError,
                $"Error validating job end: {ex.Message}");
        }
    }

    /// <summary>
    /// Validate job completion with quantity thresholds
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="minimumCompletionPercentage">Minimum completion percentage (default: 80%)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with completion analysis</returns>
    public async Task<JobCompletionValidationResult> ValidateJobCompletionAsync(
        string workOrderId,
        decimal minimumCompletionPercentage = 80m,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Validating job completion for work order {WorkOrderId} with minimum {MinimumPercentage}%",
            workOrderId, minimumCompletionPercentage);

        try
        {
            var workOrder = await _workOrderRepository.GetByIdAsync(workOrderId, cancellationToken);
            if (workOrder == null)
            {
                return JobCompletionValidationResult.Failure(
                    $"Work order '{workOrderId}' does not exist");
            }

            if (!workOrder.IsActive)
            {
                return JobCompletionValidationResult.Failure(
                    $"Work order '{workOrderId}' is not active (status: {workOrder.Status})");
            }

            var completionPercentage = workOrder.GetCompletionPercentage();

            // Check for under-completion
            if (completionPercentage < minimumCompletionPercentage)
            {
                return JobCompletionValidationResult.RequiresReason(
                    JobCompletionIssueType.UnderCompletion,
                    $"Job only {completionPercentage:F1}% complete. Minimum required: {minimumCompletionPercentage:F1}%. Provide reason for early completion.",
                    workOrder.PlannedQuantity,
                    workOrder.TotalQuantityProduced,
                    completionPercentage);
            }

            // Check for significant overproduction (>110%)
            if (completionPercentage > 110m)
            {
                return JobCompletionValidationResult.RequiresReason(
                    JobCompletionIssueType.Overproduction,
                    $"Job completed {completionPercentage:F1}% of planned quantity. Explain overproduction.",
                    workOrder.PlannedQuantity,
                    workOrder.TotalQuantityProduced,
                    completionPercentage);
            }

            _logger.LogDebug("Job completion validation passed for work order {WorkOrderId} ({CompletionPercentage:F1}%)",
                workOrderId, completionPercentage);

            return JobCompletionValidationResult.Success(
                workOrder.PlannedQuantity,
                workOrder.TotalQuantityProduced,
                completionPercentage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating job completion for work order {WorkOrderId}", workOrderId);

            return JobCompletionValidationResult.Failure(
                $"Error validating job completion: {ex.Message}");
        }
    }

    /// <summary>
    /// Get the currently active job on an equipment line
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Active work order or null if none</returns>
    public async Task<WorkOrder?> GetActiveJobOnLineAsync(
        string lineId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Map line ID to device ID for repository query
            var equipmentLine = await _equipmentLineRepository.GetByLineIdAsync(lineId, cancellationToken);
            if (equipmentLine == null)
            {
                _logger.LogWarning("Equipment line {LineId} not found when checking for active job", lineId);
                return null;
            }

            // Get active work order for the ADAM device
            var activeJob = await _workOrderRepository.GetActiveByDeviceAsync(equipmentLine.AdamDeviceId, cancellationToken);

            _logger.LogDebug("Active job check for line {LineId}: {Result}",
                lineId, activeJob?.Id ?? "None");

            return activeJob;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active job for line {LineId}", lineId);
            return null;
        }
    }

    /// <summary>
    /// Check if an equipment line is available for a new job
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if available, false otherwise</returns>
    public async Task<bool> IsLineAvailableForNewJobAsync(
        string lineId,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateJobStartAsync(lineId, "temp-validation-id", cancellationToken);
        return validation.IsValid && validation.ViolationType != JobSequencingViolationType.WorkOrderAlreadyExists;
    }

    /// <summary>
    /// Get equipment line status for all lines
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of line statuses</returns>
    public async Task<IEnumerable<EquipmentLineStatus>> GetEquipmentLineStatusesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var equipmentLines = await _equipmentLineRepository.GetActiveAsync(cancellationToken);
            var statuses = new List<EquipmentLineStatus>();

            foreach (var line in equipmentLines)
            {
                var activeJob = await GetActiveJobOnLineAsync(line.LineId, cancellationToken);
                var isAvailable = activeJob == null;

                statuses.Add(new EquipmentLineStatus(
                    line.LineId,
                    line.LineName,
                    isAvailable,
                    activeJob?.Id,
                    activeJob?.ProductDescription,
                    activeJob?.GetCompletionPercentage()
                ));
            }

            return statuses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting equipment line statuses");
            return Enumerable.Empty<EquipmentLineStatus>();
        }
    }
}

/// <summary>
/// Job sequencing validation result
/// </summary>
public class JobSequencingValidationResult
{
    /// <summary>
    /// Whether the validation passed successfully
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Type of violation if validation failed
    /// </summary>
    public JobSequencingViolationType? ViolationType { get; private set; }

    /// <summary>
    /// Error message describing the validation failure
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Conflicting work order that caused the validation failure (if applicable)
    /// </summary>
    public WorkOrder? ConflictingWorkOrder { get; private set; }

    private JobSequencingValidationResult(bool isValid, JobSequencingViolationType? violationType = null, string? errorMessage = null, WorkOrder? conflictingWorkOrder = null)
    {
        IsValid = isValid;
        ViolationType = violationType;
        ErrorMessage = errorMessage;
        ConflictingWorkOrder = conflictingWorkOrder;
    }

    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    /// <returns>Successful validation result</returns>
    public static JobSequencingValidationResult Success() => new(true);

    /// <summary>
    /// Creates a failed validation result with violation details
    /// </summary>
    /// <param name="violationType">Type of violation that occurred</param>
    /// <param name="errorMessage">Error message describing the failure</param>
    /// <param name="conflictingWorkOrder">Conflicting work order (if applicable)</param>
    /// <returns>Failed validation result</returns>
    public static JobSequencingValidationResult Failure(JobSequencingViolationType violationType, string errorMessage, WorkOrder? conflictingWorkOrder = null) =>
        new(false, violationType, errorMessage, conflictingWorkOrder);
}

/// <summary>
/// Job completion validation result
/// </summary>
public class JobCompletionValidationResult
{
    /// <summary>
    /// Whether the validation passed successfully
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Whether the completion requires a reason code
    /// </summary>
    public bool RequiresReasonCode { get; private set; }

    /// <summary>
    /// Type of completion issue (if any)
    /// </summary>
    public JobCompletionIssueType? IssueType { get; private set; }

    /// <summary>
    /// Message describing the validation result
    /// </summary>
    public string? Message { get; private set; }

    /// <summary>
    /// Planned quantity for the job
    /// </summary>
    public decimal? PlannedQuantity { get; private set; }

    /// <summary>
    /// Actual quantity produced
    /// </summary>
    public decimal? ActualQuantity { get; private set; }

    /// <summary>
    /// Calculated completion percentage
    /// </summary>
    public decimal? CompletionPercentage { get; private set; }

    private JobCompletionValidationResult(
        bool isValid,
        bool requiresReasonCode = false,
        JobCompletionIssueType? issueType = null,
        string? message = null,
        decimal? plannedQuantity = null,
        decimal? actualQuantity = null,
        decimal? completionPercentage = null)
    {
        IsValid = isValid;
        RequiresReasonCode = requiresReasonCode;
        IssueType = issueType;
        Message = message;
        PlannedQuantity = plannedQuantity;
        ActualQuantity = actualQuantity;
        CompletionPercentage = completionPercentage;
    }

    /// <summary>
    /// Creates a successful completion validation result
    /// </summary>
    /// <param name="plannedQuantity">Planned quantity for the job</param>
    /// <param name="actualQuantity">Actual quantity produced</param>
    /// <param name="completionPercentage">Completion percentage</param>
    /// <returns>Successful validation result</returns>
    public static JobCompletionValidationResult Success(decimal plannedQuantity, decimal actualQuantity, decimal completionPercentage) =>
        new(true, false, null, null, plannedQuantity, actualQuantity, completionPercentage);

    /// <summary>
    /// Creates a validation result that requires a reason code
    /// </summary>
    /// <param name="issueType">Type of completion issue</param>
    /// <param name="message">Message explaining why reason is required</param>
    /// <param name="plannedQuantity">Planned quantity for the job</param>
    /// <param name="actualQuantity">Actual quantity produced</param>
    /// <param name="completionPercentage">Completion percentage</param>
    /// <returns>Validation result requiring reason code</returns>
    public static JobCompletionValidationResult RequiresReason(JobCompletionIssueType issueType, string message, decimal plannedQuantity, decimal actualQuantity, decimal completionPercentage) =>
        new(false, true, issueType, message, plannedQuantity, actualQuantity, completionPercentage);

    /// <summary>
    /// Creates a failed validation result
    /// </summary>
    /// <param name="message">Error message describing the failure</param>
    /// <returns>Failed validation result</returns>
    public static JobCompletionValidationResult Failure(string message) =>
        new(false, false, null, message);
}

/// <summary>
/// Types of job sequencing violations
/// </summary>
public enum JobSequencingViolationType
{
    /// <summary>
    /// Equipment line not found
    /// </summary>
    EquipmentLineNotFound,

    /// <summary>
    /// Equipment line is inactive
    /// </summary>
    EquipmentLineInactive,

    /// <summary>
    /// Another job is already running on the line
    /// </summary>
    OverlappingJob,

    /// <summary>
    /// Work order already exists
    /// </summary>
    WorkOrderAlreadyExists,

    /// <summary>
    /// Work order not found
    /// </summary>
    WorkOrderNotFound,

    /// <summary>
    /// Work order is not active
    /// </summary>
    WorkOrderNotActive,

    /// <summary>
    /// Validation error occurred
    /// </summary>
    ValidationError
}

/// <summary>
/// Types of job completion issues
/// </summary>
public enum JobCompletionIssueType
{
    /// <summary>
    /// Job completed with less than required minimum percentage
    /// </summary>
    UnderCompletion,

    /// <summary>
    /// Job completed with significantly more than planned quantity
    /// </summary>
    Overproduction
}

/// <summary>
/// Equipment line status information
/// </summary>
/// <param name="LineId">Equipment line identifier</param>
/// <param name="LineName">Equipment line name</param>
/// <param name="IsAvailable">Whether available for new job</param>
/// <param name="ActiveWorkOrderId">Active work order ID (if any)</param>
/// <param name="ActiveProduct">Active product description (if any)</param>
/// <param name="CompletionPercentage">Completion percentage of active job</param>
public record EquipmentLineStatus(
    string LineId,
    string LineName,
    bool IsAvailable,
    string? ActiveWorkOrderId,
    string? ActiveProduct,
    decimal? CompletionPercentage
);
