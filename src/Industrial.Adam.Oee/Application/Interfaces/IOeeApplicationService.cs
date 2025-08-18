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
    public string DeviceId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal CurrentRate { get; set; }
    public bool HasActiveWorkOrder { get; set; }
    public string? ActiveWorkOrderId { get; set; }
    public bool IsInStoppage { get; set; }
    public decimal? StoppageDurationMinutes { get; set; }
    public DateTime LastUpdate { get; set; }
}

/// <summary>
/// Work order validation result
/// </summary>
public class WorkOrderValidationResult
{
    public bool IsValid { get; set; }
    public IEnumerable<string> Issues { get; set; } = Array.Empty<string>();
    public IEnumerable<string> Warnings { get; set; } = Array.Empty<string>();
    public bool CanProceed { get; set; }
    public DateTime ValidationTimestamp { get; set; }
}
