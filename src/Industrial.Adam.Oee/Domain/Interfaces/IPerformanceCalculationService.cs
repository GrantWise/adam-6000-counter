using Industrial.Adam.Oee.Domain.ValueObjects;

namespace Industrial.Adam.Oee.Domain.Interfaces;

/// <summary>
/// Domain service for performance calculations
/// Handles all performance-related OEE calculations
/// </summary>
public interface IPerformanceCalculationService
{
    /// <summary>
    /// Calculate performance from counter data and target rates
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="startTime">Period start time</param>
    /// <param name="endTime">Period end time</param>
    /// <param name="targetRatePerMinute">Target production rate per minute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Performance calculation</returns>
    public Task<Performance> CalculateAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        decimal targetRatePerMinute,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate performance for a work order
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Performance calculation</returns>
    public Task<Performance> CalculateForWorkOrderAsync(
        string workOrderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate performance with custom target configuration
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="startTime">Period start time</param>
    /// <param name="endTime">Period end time</param>
    /// <param name="configuration">Performance calculation configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Performance calculation</returns>
    public Task<Performance> CalculateWithConfigurationAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        PerformanceConfiguration configuration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get performance trends over multiple periods
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="startTime">Overall period start</param>
    /// <param name="endTime">Overall period end</param>
    /// <param name="intervalMinutes">Interval size in minutes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Performance trends</returns>
    public Task<IEnumerable<PerformanceTrend>> GetTrendsAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        int intervalMinutes,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Configuration for performance calculations
/// </summary>
/// <param name="TargetRatePerMinute">Target production rate per minute</param>
/// <param name="ProductionChannel">Channel number for production counting</param>
/// <param name="UseWeightedAveraging">Whether to use weighted averaging for rate calculations</param>
public record PerformanceConfiguration(
    decimal TargetRatePerMinute,
    int ProductionChannel = 0,
    bool UseWeightedAveraging = true
);

/// <summary>
/// Performance trend data point
/// </summary>
/// <param name="Time">Time point</param>
/// <param name="Performance">Performance at this time</param>
public record PerformanceTrend(
    DateTime Time,
    Performance Performance
);
