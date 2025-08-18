namespace Industrial.Adam.Oee.Domain.Interfaces;

/// <summary>
/// Repository interface for accessing counter data from TimescaleDB
/// </summary>
public interface ICounterDataRepository
{
    /// <summary>
    /// Get counter readings for a specific device and time period
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="startTime">Start of time period</param>
    /// <param name="endTime">End of time period</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of counter readings</returns>
    public Task<IEnumerable<CounterReading>> GetDataForPeriodAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the latest counter reading for a specific device and channel
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="channel">Channel number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Latest counter reading or null if not found</returns>
    public Task<CounterReading?> GetLatestReadingAsync(
        string deviceId,
        int channel,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get counter readings for multiple channels within a time period
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="channels">Channel numbers to retrieve</param>
    /// <param name="startTime">Start of time period</param>
    /// <param name="endTime">End of time period</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of counter readings</returns>
    public Task<IEnumerable<CounterReading>> GetDataForChannelsAsync(
        string deviceId,
        IEnumerable<int> channels,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get aggregated counter data for performance calculations
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="channel">Channel number</param>
    /// <param name="startTime">Start of time period</param>
    /// <param name="endTime">End of time period</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Aggregated counter statistics</returns>
    public Task<CounterAggregates?> GetAggregatedDataAsync(
        string deviceId,
        int channel,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the current production rate for a device
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="channel">Channel number</param>
    /// <param name="lookbackMinutes">Lookback period in minutes for rate calculation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current production rate per minute</returns>
    public Task<decimal> GetCurrentRateAsync(
        string deviceId,
        int channel,
        int lookbackMinutes = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a device has been producing (non-zero rate) within a time period
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="channel">Channel number</param>
    /// <param name="startTime">Start of time period</param>
    /// <param name="endTime">End of time period</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if device was producing, false otherwise</returns>
    public Task<bool> HasProductionActivityAsync(
        string deviceId,
        int channel,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get downtime periods where production was stopped
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="channel">Channel number</param>
    /// <param name="startTime">Start of time period</param>
    /// <param name="endTime">End of time period</param>
    /// <param name="minimumStoppageMinutes">Minimum stoppage duration to be considered downtime</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of downtime periods</returns>
    public Task<IEnumerable<DowntimePeriod>> GetDowntimePeriodsAsync(
        string deviceId,
        int channel,
        DateTime startTime,
        DateTime endTime,
        int minimumStoppageMinutes = 5,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a counter reading from TimescaleDB
/// </summary>
/// <param name="DeviceId">Device identifier</param>
/// <param name="Channel">Channel number</param>
/// <param name="Timestamp">Reading timestamp</param>
/// <param name="Rate">Production rate (pieces per second)</param>
/// <param name="ProcessedValue">Cumulative count value</param>
/// <param name="Quality">Quality indicator</param>
public record CounterReading(
    string DeviceId,
    int Channel,
    DateTime Timestamp,
    decimal Rate,
    decimal ProcessedValue,
    string? Quality = null
);

/// <summary>
/// Aggregated counter statistics for a time period
/// </summary>
/// <param name="DeviceId">Device identifier</param>
/// <param name="Channel">Channel number</param>
/// <param name="StartTime">Start of aggregation period</param>
/// <param name="EndTime">End of aggregation period</param>
/// <param name="TotalCount">Total count change over period</param>
/// <param name="AverageRate">Average production rate</param>
/// <param name="MaxRate">Maximum production rate</param>
/// <param name="MinRate">Minimum production rate</param>
/// <param name="RunTimeMinutes">Time with non-zero rate</param>
/// <param name="DataPoints">Number of data points in aggregation</param>
public record CounterAggregates(
    string DeviceId,
    int Channel,
    DateTime StartTime,
    DateTime EndTime,
    decimal TotalCount,
    decimal AverageRate,
    decimal MaxRate,
    decimal MinRate,
    decimal RunTimeMinutes,
    int DataPoints
);

/// <summary>
/// Represents a downtime period
/// </summary>
/// <param name="StartTime">When downtime started</param>
/// <param name="EndTime">When downtime ended (null if ongoing)</param>
/// <param name="DurationMinutes">Duration in minutes</param>
/// <param name="IsOngoing">Whether downtime is still ongoing</param>
public record DowntimePeriod(
    DateTime StartTime,
    DateTime? EndTime,
    decimal DurationMinutes,
    bool IsOngoing
);
