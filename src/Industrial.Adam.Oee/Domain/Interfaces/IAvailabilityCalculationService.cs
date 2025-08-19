using Industrial.Adam.Oee.Domain.ValueObjects;

namespace Industrial.Adam.Oee.Domain.Interfaces;

/// <summary>
/// Domain service for availability calculations
/// Handles all availability-related OEE calculations following Single Responsibility Principle
/// </summary>
public interface IAvailabilityCalculationService
{
    /// <summary>
    /// Calculate availability from counter data and downtime records
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="startTime">Period start time</param>
    /// <param name="endTime">Period end time</param>
    /// <param name="downtimeRecords">Optional downtime records</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Availability calculation</returns>
    public Task<Availability> CalculateAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        IEnumerable<DowntimeRecord>? downtimeRecords = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detect downtime periods for a device
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="startTime">Period start time</param>
    /// <param name="endTime">Period end time</param>
    /// <param name="minimumStoppageMinutes">Minimum stoppage duration in minutes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of downtime periods</returns>
    public Task<IEnumerable<DowntimePeriod>> DetectDowntimeAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        int minimumStoppageMinutes = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate actual runtime from counter data
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="startTime">Period start time</param>
    /// <param name="endTime">Period end time</param>
    /// <param name="productionChannel">Production channel number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Actual runtime in minutes</returns>
    public Task<decimal> CalculateActualRuntimeAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        int productionChannel = 0,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Planned downtime period
/// </summary>
/// <param name="StartTime">Start of planned downtime</param>
/// <param name="EndTime">End of planned downtime</param>
/// <param name="Reason">Reason for planned downtime</param>
public record PlannedDowntime(
    DateTime StartTime,
    DateTime EndTime,
    string Reason
);

/// <summary>
/// Availability trend data point
/// </summary>
/// <param name="Time">Time point</param>
/// <param name="Availability">Availability at this time</param>
public record AvailabilityTrend(
    DateTime Time,
    Availability Availability
);
