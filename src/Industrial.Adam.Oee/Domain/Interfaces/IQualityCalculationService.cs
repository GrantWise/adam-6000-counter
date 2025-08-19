using Industrial.Adam.Oee.Domain.ValueObjects;

namespace Industrial.Adam.Oee.Domain.Interfaces;

/// <summary>
/// Domain service for quality calculations
/// Handles all quality-related OEE calculations
/// </summary>
public interface IQualityCalculationService
{
    /// <summary>
    /// Calculate quality from counter data (good vs. defective pieces)
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="startTime">Period start time</param>
    /// <param name="endTime">Period end time</param>
    /// <param name="productionChannel">Channel number for good pieces</param>
    /// <param name="rejectChannel">Channel number for defective pieces</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Quality calculation</returns>
    public Task<Quality> CalculateAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        int productionChannel = 0,
        int rejectChannel = 1,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate quality for a work order
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Quality calculation</returns>
    public Task<Quality> CalculateForWorkOrderAsync(
        string workOrderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate quality with custom channel configuration
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="startTime">Period start time</param>
    /// <param name="endTime">Period end time</param>
    /// <param name="configuration">Quality calculation configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Quality calculation</returns>
    public Task<Quality> CalculateWithConfigurationAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        QualityConfiguration configuration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get quality trends over multiple periods
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="startTime">Overall period start</param>
    /// <param name="endTime">Overall period end</param>
    /// <param name="intervalMinutes">Interval size in minutes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Quality trends</returns>
    public Task<IEnumerable<QualityTrend>> GetTrendsAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        int intervalMinutes,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Configuration for quality calculations
/// </summary>
/// <param name="ProductionChannel">Channel number for production counting</param>
/// <param name="RejectChannel">Channel number for reject counting</param>
/// <param name="QualityGates">Optional quality gates for additional validation</param>
public record QualityConfiguration(
    int ProductionChannel = 0,
    int RejectChannel = 1,
    IEnumerable<QualityGate>? QualityGates = null
);

/// <summary>
/// Quality gate configuration
/// </summary>
/// <param name="Name">Quality gate name</param>
/// <param name="Threshold">Quality threshold percentage</param>
/// <param name="AlertLevel">Alert level when threshold is breached</param>
public record QualityGate(
    string Name,
    decimal Threshold,
    string AlertLevel
);

/// <summary>
/// Quality trend data point
/// </summary>
/// <param name="Time">Time point</param>
/// <param name="Quality">Quality at this time</param>
public record QualityTrend(
    DateTime Time,
    Quality Quality
);
