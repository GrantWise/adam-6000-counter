using Industrial.Adam.Oee.Domain.Entities;
using Industrial.Adam.Oee.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Domain.Services;

/// <summary>
/// Domain service for equipment line and ADAM device mapping operations
/// 
/// Provides business logic for:
/// - ADAM device to equipment line mapping
/// - Equipment configuration validation
/// - Line availability and status checking
/// </summary>
public class EquipmentLineService : IEquipmentLineService
{
    private readonly IEquipmentLineRepository _equipmentLineRepository;
    private readonly IWorkOrderRepository _workOrderRepository;
    private readonly ILogger<EquipmentLineService> _logger;

    /// <summary>
    /// Initializes a new instance of the EquipmentLineService
    /// </summary>
    /// <param name="equipmentLineRepository">Repository for equipment line operations</param>
    /// <param name="workOrderRepository">Repository for work order operations</param>
    /// <param name="logger">Logger instance for service operations</param>
    public EquipmentLineService(
        IEquipmentLineRepository equipmentLineRepository,
        IWorkOrderRepository workOrderRepository,
        ILogger<EquipmentLineService> logger)
    {
        _equipmentLineRepository = equipmentLineRepository;
        _workOrderRepository = workOrderRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get equipment line by ADAM device and channel
    /// </summary>
    /// <param name="adamDeviceId">ADAM device identifier</param>
    /// <param name="adamChannel">ADAM channel number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Equipment line or null if not found</returns>
    public async Task<EquipmentLine?> GetEquipmentLineByAdamDeviceAsync(
        string adamDeviceId,
        int adamChannel,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting equipment line for ADAM device {AdamDeviceId}:{AdamChannel}",
            adamDeviceId, adamChannel);

        try
        {
            var equipmentLine = await _equipmentLineRepository.GetByAdamDeviceAsync(adamDeviceId, adamChannel, cancellationToken);

            if (equipmentLine == null)
            {
                _logger.LogWarning("No equipment line found for ADAM device {AdamDeviceId}:{AdamChannel}",
                    adamDeviceId, adamChannel);
            }
            else
            {
                _logger.LogDebug("Found equipment line {LineId} for ADAM device {AdamDeviceId}:{AdamChannel}",
                    equipmentLine.LineId, adamDeviceId, adamChannel);
            }

            return equipmentLine;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting equipment line for ADAM device {AdamDeviceId}:{AdamChannel}",
                adamDeviceId, adamChannel);
            return null;
        }
    }

    /// <summary>
    /// Validate work order equipment assignment
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    public async Task<EquipmentValidationResult> ValidateWorkOrderEquipmentAsync(
        string workOrderId,
        string lineId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Validating equipment assignment for work order {WorkOrderId} on line {LineId}",
            workOrderId, lineId);

        try
        {
            // Check if equipment line exists and is active
            var equipmentLine = await _equipmentLineRepository.GetByLineIdAsync(lineId, cancellationToken);
            if (equipmentLine == null)
            {
                return EquipmentValidationResult.Failure(
                    EquipmentValidationType.LineNotFound,
                    $"Equipment line '{lineId}' does not exist");
            }

            if (!equipmentLine.IsActive)
            {
                return EquipmentValidationResult.Failure(
                    EquipmentValidationType.LineInactive,
                    $"Equipment line '{lineId}' is not active");
            }

            // Check if there's already an active job on this line
            var activeJob = await _workOrderRepository.GetActiveByDeviceAsync(equipmentLine.AdamDeviceId, cancellationToken);
            if (activeJob != null && activeJob.Id != workOrderId)
            {
                return EquipmentValidationResult.Failure(
                    EquipmentValidationType.LineOccupied,
                    $"Equipment line '{lineId}' is already occupied by work order '{activeJob.Id}'",
                    activeJob);
            }

            _logger.LogDebug("Equipment validation passed for work order {WorkOrderId} on line {LineId}",
                workOrderId, lineId);

            return EquipmentValidationResult.Success(equipmentLine);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating equipment assignment for work order {WorkOrderId} on line {LineId}",
                workOrderId, lineId);

            return EquipmentValidationResult.Failure(
                EquipmentValidationType.ValidationError,
                $"Error validating equipment assignment: {ex.Message}");
        }
    }

    /// <summary>
    /// Get all ADAM device mappings
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of ADAM device mappings</returns>
    public async Task<IEnumerable<Entities.AdamDeviceMapping>> GetAllAdamDeviceMappingsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _equipmentLineRepository.GetAdamDeviceMappingsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ADAM device mappings");
            return Enumerable.Empty<Entities.AdamDeviceMapping>();
        }
    }

    /// <summary>
    /// Check if ADAM device mapping is available
    /// </summary>
    /// <param name="adamDeviceId">ADAM device identifier</param>
    /// <param name="adamChannel">ADAM channel number</param>
    /// <param name="excludeLineId">Line ID to exclude from check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if mapping is available</returns>
    public async Task<bool> IsAdamDeviceMappingAvailableAsync(
        string adamDeviceId,
        int adamChannel,
        string? excludeLineId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _equipmentLineRepository.IsAdamDeviceMappingAvailableAsync(
                adamDeviceId, adamChannel, excludeLineId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking ADAM device mapping availability for {AdamDeviceId}:{AdamChannel}",
                adamDeviceId, adamChannel);
            return false;
        }
    }

    /// <summary>
    /// Create new equipment line with validation
    /// </summary>
    /// <param name="creationData">Equipment line creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Creation result</returns>
    public async Task<EquipmentLineCreationResult> CreateEquipmentLineAsync(
        EquipmentLineCreationData creationData,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating equipment line {LineId} for ADAM device {AdamDeviceId}:{AdamChannel}",
            creationData.LineId, creationData.AdamDeviceId, creationData.AdamChannel);

        try
        {
            // Validate line ID is not already in use
            var existingLine = await _equipmentLineRepository.GetByLineIdAsync(creationData.LineId, cancellationToken);
            if (existingLine != null)
            {
                return EquipmentLineCreationResult.Failure(
                    $"Equipment line ID '{creationData.LineId}' is already in use");
            }

            // Validate ADAM device mapping is available
            var isMappingAvailable = await IsAdamDeviceMappingAvailableAsync(
                creationData.AdamDeviceId, creationData.AdamChannel, null, cancellationToken);

            if (!isMappingAvailable)
            {
                return EquipmentLineCreationResult.Failure(
                    $"ADAM device {creationData.AdamDeviceId}:{creationData.AdamChannel} is already mapped to another equipment line");
            }

            // Create equipment line entity
            var equipmentLine = new EquipmentLine(
                creationData.LineId,
                creationData.LineName,
                creationData.AdamDeviceId,
                creationData.AdamChannel,
                creationData.IsActive);

            // Save to repository
            var id = await _equipmentLineRepository.CreateAsync(equipmentLine, cancellationToken);

            _logger.LogInformation("Successfully created equipment line {LineId} with ID {Id}",
                creationData.LineId, id);

            return EquipmentLineCreationResult.Success(id, equipmentLine);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating equipment line {LineId}", creationData.LineId);

            return EquipmentLineCreationResult.Failure(
                $"Error creating equipment line: {ex.Message}");
        }
    }

    /// <summary>
    /// Update equipment line ADAM device mapping
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="newAdamDeviceId">New ADAM device identifier</param>
    /// <param name="newAdamChannel">New ADAM channel number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Update result</returns>
    public async Task<EquipmentLineUpdateResult> UpdateAdamDeviceMappingAsync(
        string lineId,
        string newAdamDeviceId,
        int newAdamChannel,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating ADAM device mapping for equipment line {LineId} to {AdamDeviceId}:{AdamChannel}",
            lineId, newAdamDeviceId, newAdamChannel);

        try
        {
            // Get existing equipment line
            var equipmentLine = await _equipmentLineRepository.GetByLineIdAsync(lineId, cancellationToken);
            if (equipmentLine == null)
            {
                return EquipmentLineUpdateResult.Failure(
                    $"Equipment line '{lineId}' does not exist");
            }

            // Check if there's an active job on this line
            var activeJob = await _workOrderRepository.GetActiveByDeviceAsync(equipmentLine.AdamDeviceId, cancellationToken);
            if (activeJob != null)
            {
                return EquipmentLineUpdateResult.Failure(
                    $"Cannot update ADAM device mapping while equipment line has an active job: {activeJob.Id}");
            }

            // Validate new ADAM device mapping is available
            var isMappingAvailable = await IsAdamDeviceMappingAvailableAsync(
                newAdamDeviceId, newAdamChannel, lineId, cancellationToken);

            if (!isMappingAvailable)
            {
                return EquipmentLineUpdateResult.Failure(
                    $"ADAM device {newAdamDeviceId}:{newAdamChannel} is already mapped to another equipment line");
            }

            // Update equipment line
            equipmentLine.UpdateAdamMapping(newAdamDeviceId, newAdamChannel);
            var updated = await _equipmentLineRepository.UpdateAsync(equipmentLine, cancellationToken);

            if (!updated)
            {
                return EquipmentLineUpdateResult.Failure(
                    $"Failed to update equipment line '{lineId}'");
            }

            _logger.LogInformation("Successfully updated ADAM device mapping for equipment line {LineId}", lineId);

            return EquipmentLineUpdateResult.Success(equipmentLine);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating ADAM device mapping for equipment line {LineId}", lineId);

            return EquipmentLineUpdateResult.Failure(
                $"Error updating ADAM device mapping: {ex.Message}");
        }
    }

    /// <summary>
    /// Get equipment line availability status
    /// </summary>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Equipment line availability status</returns>
    public async Task<EquipmentLineAvailability?> GetEquipmentLineAvailabilityAsync(
        string lineId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var equipmentLine = await _equipmentLineRepository.GetByLineIdAsync(lineId, cancellationToken);
            if (equipmentLine == null)
            {
                return null;
            }

            var activeJob = await _workOrderRepository.GetActiveByDeviceAsync(equipmentLine.AdamDeviceId, cancellationToken);
            var isAvailable = equipmentLine.IsActive && activeJob == null;

            return new EquipmentLineAvailability(
                equipmentLine.LineId,
                equipmentLine.LineName,
                equipmentLine.IsActive,
                isAvailable,
                activeJob?.Id,
                activeJob?.ProductDescription,
                activeJob?.GetCompletionPercentage(),
                equipmentLine.AdamDeviceId,
                equipmentLine.AdamChannel
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting equipment line availability for {LineId}", lineId);
            return null;
        }
    }

    /// <summary>
    /// Get all equipment line availability statuses
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of equipment line availability statuses</returns>
    public async Task<IEnumerable<EquipmentLineAvailability>> GetAllEquipmentLineAvailabilitiesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var equipmentLines = await _equipmentLineRepository.GetAllAsync(cancellationToken);
            var availabilities = new List<EquipmentLineAvailability>();

            foreach (var line in equipmentLines)
            {
                var activeJob = await _workOrderRepository.GetActiveByDeviceAsync(line.AdamDeviceId, cancellationToken);
                var isAvailable = line.IsActive && activeJob == null;

                availabilities.Add(new EquipmentLineAvailability(
                    line.LineId,
                    line.LineName,
                    line.IsActive,
                    isAvailable,
                    activeJob?.Id,
                    activeJob?.ProductDescription,
                    activeJob?.GetCompletionPercentage(),
                    line.AdamDeviceId,
                    line.AdamChannel
                ));
            }

            return availabilities;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all equipment line availabilities");
            return Enumerable.Empty<EquipmentLineAvailability>();
        }
    }
}

/// <summary>
/// Equipment validation result
/// </summary>
public class EquipmentValidationResult
{
    /// <summary>
    /// Whether the validation passed successfully
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Type of validation issue if validation failed
    /// </summary>
    public EquipmentValidationType? ValidationType { get; private set; }

    /// <summary>
    /// Error message describing the validation failure
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// The validated equipment line (if validation succeeded)
    /// </summary>
    public EquipmentLine? EquipmentLine { get; private set; }

    /// <summary>
    /// Conflicting work order that caused the validation failure (if applicable)
    /// </summary>
    public WorkOrder? ConflictingWorkOrder { get; private set; }

    private EquipmentValidationResult(
        bool isValid,
        EquipmentValidationType? validationType = null,
        string? errorMessage = null,
        EquipmentLine? equipmentLine = null,
        WorkOrder? conflictingWorkOrder = null)
    {
        IsValid = isValid;
        ValidationType = validationType;
        ErrorMessage = errorMessage;
        EquipmentLine = equipmentLine;
        ConflictingWorkOrder = conflictingWorkOrder;
    }

    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    /// <param name="equipmentLine">The validated equipment line</param>
    /// <returns>Successful validation result</returns>
    public static EquipmentValidationResult Success(EquipmentLine equipmentLine) =>
        new(true, null, null, equipmentLine);

    /// <summary>
    /// Creates a failed validation result with issue details
    /// </summary>
    /// <param name="validationType">Type of validation issue that occurred</param>
    /// <param name="errorMessage">Error message describing the failure</param>
    /// <param name="conflictingWorkOrder">Conflicting work order (if applicable)</param>
    /// <returns>Failed validation result</returns>
    public static EquipmentValidationResult Failure(EquipmentValidationType validationType, string errorMessage, WorkOrder? conflictingWorkOrder = null) =>
        new(false, validationType, errorMessage, null, conflictingWorkOrder);
}

/// <summary>
/// Equipment line creation result
/// </summary>
public class EquipmentLineCreationResult
{
    /// <summary>
    /// Whether the creation was successful
    /// </summary>
    public bool IsSuccess { get; private set; }

    /// <summary>
    /// Error message if creation failed
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Database identifier of the created equipment line
    /// </summary>
    public int? EquipmentLineId { get; private set; }

    /// <summary>
    /// The created equipment line entity
    /// </summary>
    public EquipmentLine? EquipmentLine { get; private set; }

    private EquipmentLineCreationResult(bool isSuccess, string? errorMessage = null, int? equipmentLineId = null, EquipmentLine? equipmentLine = null)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        EquipmentLineId = equipmentLineId;
        EquipmentLine = equipmentLine;
    }

    /// <summary>
    /// Creates a successful creation result
    /// </summary>
    /// <param name="equipmentLineId">Database identifier of the created equipment line</param>
    /// <param name="equipmentLine">The created equipment line entity</param>
    /// <returns>Successful creation result</returns>
    public static EquipmentLineCreationResult Success(int equipmentLineId, EquipmentLine equipmentLine) =>
        new(true, null, equipmentLineId, equipmentLine);

    /// <summary>
    /// Creates a failed creation result
    /// </summary>
    /// <param name="errorMessage">Error message describing the failure</param>
    /// <returns>Failed creation result</returns>
    public static EquipmentLineCreationResult Failure(string errorMessage) =>
        new(false, errorMessage);
}

/// <summary>
/// Equipment line update result
/// </summary>
public class EquipmentLineUpdateResult
{
    /// <summary>
    /// Whether the update was successful
    /// </summary>
    public bool IsSuccess { get; private set; }

    /// <summary>
    /// Error message if update failed
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// The updated equipment line entity
    /// </summary>
    public EquipmentLine? EquipmentLine { get; private set; }

    private EquipmentLineUpdateResult(bool isSuccess, string? errorMessage = null, EquipmentLine? equipmentLine = null)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        EquipmentLine = equipmentLine;
    }

    /// <summary>
    /// Creates a successful update result
    /// </summary>
    /// <param name="equipmentLine">The updated equipment line entity</param>
    /// <returns>Successful update result</returns>
    public static EquipmentLineUpdateResult Success(EquipmentLine equipmentLine) =>
        new(true, null, equipmentLine);

    /// <summary>
    /// Creates a failed update result
    /// </summary>
    /// <param name="errorMessage">Error message describing the failure</param>
    /// <returns>Failed update result</returns>
    public static EquipmentLineUpdateResult Failure(string errorMessage) =>
        new(false, errorMessage);
}

/// <summary>
/// Types of equipment validation issues
/// </summary>
public enum EquipmentValidationType
{
    /// <summary>
    /// Equipment line not found
    /// </summary>
    LineNotFound,

    /// <summary>
    /// Equipment line is inactive
    /// </summary>
    LineInactive,

    /// <summary>
    /// Equipment line is occupied by another job
    /// </summary>
    LineOccupied,

    /// <summary>
    /// Validation error occurred
    /// </summary>
    ValidationError
}

/// <summary>
/// Equipment line availability information
/// </summary>
/// <param name="LineId">Equipment line identifier</param>
/// <param name="LineName">Equipment line name</param>
/// <param name="IsActive">Whether line is active</param>
/// <param name="IsAvailable">Whether available for new job</param>
/// <param name="ActiveWorkOrderId">Active work order ID (if any)</param>
/// <param name="ActiveProduct">Active product description (if any)</param>
/// <param name="CompletionPercentage">Completion percentage of active job</param>
/// <param name="AdamDeviceId">ADAM device identifier</param>
/// <param name="AdamChannel">ADAM channel number</param>
public record EquipmentLineAvailability(
    string LineId,
    string LineName,
    bool IsActive,
    bool IsAvailable,
    string? ActiveWorkOrderId,
    string? ActiveProduct,
    decimal? CompletionPercentage,
    string AdamDeviceId,
    int AdamChannel
);
