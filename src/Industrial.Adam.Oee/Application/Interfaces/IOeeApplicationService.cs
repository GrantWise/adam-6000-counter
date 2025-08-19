using Industrial.Adam.Oee.Application.DTOs;

namespace Industrial.Adam.Oee.Application.Interfaces;

/// <summary>
/// Interface for OEE Application Service
/// Defines application-level operations for OEE metrics and device management
/// </summary>
public interface IOeeApplicationService
{
    /// <summary>
    /// Get enhanced OEE metrics with additional context
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="includeWorkOrderContext">Whether to include work order context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Enhanced OEE metrics</returns>
    public Task<OeeMetricsDto> GetEnhancedOeeMetricsAsync(
        string deviceId,
        bool includeWorkOrderContext = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get device status with production context
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Device status information</returns>
    public Task<DeviceStatusDto> GetDeviceStatusAsync(string deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate work order start conditions
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    public Task<WorkOrderValidationResult> ValidateWorkOrderStartConditionsAsync(
        string deviceId,
        string workOrderId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Device status information
/// </summary>
public class DeviceStatusDto
{
    /// <summary>
    /// Device identifier
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;
    /// <summary>
    /// Current status of the device
    /// </summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>
    /// Current production rate
    /// </summary>
    public decimal CurrentRate { get; set; }
    /// <summary>
    /// Indicates if device has an active work order
    /// </summary>
    public bool HasActiveWorkOrder { get; set; }
    /// <summary>
    /// Active work order identifier, if any
    /// </summary>
    public string? ActiveWorkOrderId { get; set; }
    /// <summary>
    /// Indicates if device is currently in stoppage
    /// </summary>
    public bool IsInStoppage { get; set; }
    /// <summary>
    /// Duration of current stoppage in minutes
    /// </summary>
    public decimal? StoppageDurationMinutes { get; set; }
    /// <summary>
    /// Timestamp of last status update
    /// </summary>
    public DateTime LastUpdate { get; set; }
}

/// <summary>
/// Work order validation result
/// </summary>
public class WorkOrderValidationResult
{
    /// <summary>
    /// Indicates if the work order is valid
    /// </summary>
    public bool IsValid { get; set; }
    /// <summary>
    /// List of validation issues
    /// </summary>
    public IEnumerable<string> Issues { get; set; } = Array.Empty<string>();
    /// <summary>
    /// List of validation warnings
    /// </summary>
    public IEnumerable<string> Warnings { get; set; } = Array.Empty<string>();
    /// <summary>
    /// Indicates if processing can proceed despite issues
    /// </summary>
    public bool CanProceed { get; set; }
    /// <summary>
    /// Timestamp when validation was performed
    /// </summary>
    public DateTime ValidationTimestamp { get; set; }
}
