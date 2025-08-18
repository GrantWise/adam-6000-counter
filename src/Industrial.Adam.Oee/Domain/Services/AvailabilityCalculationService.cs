using Industrial.Adam.Oee.Domain.Enums;
using Industrial.Adam.Oee.Domain.Exceptions;
using Industrial.Adam.Oee.Domain.Interfaces;
using Industrial.Adam.Oee.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Domain.Services;

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
/// Implementation of availability calculation service
/// </summary>
public sealed class AvailabilityCalculationService : IAvailabilityCalculationService
{
    private readonly ICounterDataRepository _counterDataRepository;
    private readonly ILogger<AvailabilityCalculationService> _logger;

    /// <summary>
    /// Initialize availability calculation service
    /// </summary>
    /// <param name="counterDataRepository">Counter data repository</param>
    /// <param name="logger">Logger instance</param>
    public AvailabilityCalculationService(
        ICounterDataRepository counterDataRepository,
        ILogger<AvailabilityCalculationService> logger)
    {
        _counterDataRepository = counterDataRepository ?? throw new ArgumentNullException(nameof(counterDataRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Availability> CalculateAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        IEnumerable<DowntimeRecord>? downtimeRecords = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Device ID cannot be null or empty", nameof(deviceId));

        if (endTime <= startTime)
            throw new ArgumentException("End time must be after start time", nameof(endTime));

        _logger.LogDebug("Calculating availability for device {DeviceId} from {StartTime} to {EndTime}",
            deviceId, startTime, endTime);

        try
        {
            var plannedTimeMinutes = (decimal)(endTime - startTime).TotalMinutes;

            if (downtimeRecords != null)
            {
                _logger.LogDebug("Using provided downtime records for availability calculation");
                return Availability.FromDowntimeRecords(plannedTimeMinutes, downtimeRecords);
            }

            // Calculate actual runtime from counter data
            var actualRuntimeMinutes = await CalculateActualRuntimeAsync(
                deviceId, startTime, endTime, 0, cancellationToken);

            var availability = new Availability(plannedTimeMinutes, actualRuntimeMinutes);

            _logger.LogInformation(
                "Calculated availability for device {DeviceId}: {AvailabilityPercentage:F1}% ({ActualRuntime:F1}/{PlannedTime:F1} minutes)",
                deviceId, availability.Percentage, actualRuntimeMinutes, plannedTimeMinutes);

            return availability;
        }
        catch (Exception ex) when (!(ex is OeeCalculationException))
        {
            var calculationException = new OeeCalculationException(
                $"Failed to calculate availability for device {deviceId}",
                "Availability",
                OeeErrorCode.AvailabilityCalculationFailed,
                deviceId,
                startTime,
                endTime,
                ex);

            _logger.LogError(calculationException,
                "Availability calculation failed for device {DeviceId}", deviceId);

            throw calculationException;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DowntimePeriod>> DetectDowntimeAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        int minimumStoppageMinutes = 5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Device ID cannot be null or empty", nameof(deviceId));

        if (endTime <= startTime)
            throw new ArgumentException("End time must be after start time", nameof(endTime));

        if (minimumStoppageMinutes <= 0)
            throw new ArgumentException("Minimum stoppage minutes must be positive", nameof(minimumStoppageMinutes));

        _logger.LogDebug(
            "Detecting downtime for device {DeviceId} from {StartTime} to {EndTime} (min {MinimumMinutes} minutes)",
            deviceId, startTime, endTime, minimumStoppageMinutes);

        try
        {
            var downtimePeriods = await _counterDataRepository.GetDowntimePeriodsAsync(
                deviceId, 0, startTime, endTime, minimumStoppageMinutes, cancellationToken);

            var periodList = downtimePeriods.ToList();

            _logger.LogInformation(
                "Detected {Count} downtime periods for device {DeviceId} (total {TotalMinutes:F1} minutes)",
                periodList.Count, deviceId, periodList.Sum(p => p.DurationMinutes));

            return periodList;
        }
        catch (Exception ex) when (!(ex is OeeCalculationException))
        {
            var calculationException = new OeeCalculationException(
                $"Failed to detect downtime for device {deviceId}",
                "DowntimeDetection",
                OeeErrorCode.AvailabilityCalculationFailed,
                deviceId,
                startTime,
                endTime,
                ex);

            _logger.LogError(calculationException,
                "Downtime detection failed for device {DeviceId}", deviceId);

            throw calculationException;
        }
    }

    /// <inheritdoc />
    public async Task<decimal> CalculateActualRuntimeAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        int productionChannel = 0,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Device ID cannot be null or empty", nameof(deviceId));

        if (endTime <= startTime)
            throw new ArgumentException("End time must be after start time", nameof(endTime));

        _logger.LogDebug(
            "Calculating actual runtime for device {DeviceId} channel {Channel} from {StartTime} to {EndTime}",
            deviceId, productionChannel, startTime, endTime);

        try
        {
            var aggregates = await _counterDataRepository.GetAggregatedDataAsync(
                deviceId, productionChannel, startTime, endTime, cancellationToken);

            var actualRuntimeMinutes = aggregates?.RunTimeMinutes ?? 0;

            _logger.LogDebug(
                "Calculated actual runtime for device {DeviceId}: {Runtime:F1} minutes",
                deviceId, actualRuntimeMinutes);

            return actualRuntimeMinutes;
        }
        catch (Exception ex) when (!(ex is OeeCalculationException))
        {
            var calculationException = new OeeCalculationException(
                $"Failed to calculate actual runtime for device {deviceId}",
                "ActualRuntime",
                OeeErrorCode.AvailabilityCalculationFailed,
                deviceId,
                startTime,
                endTime,
                ex);

            _logger.LogError(calculationException,
                "Actual runtime calculation failed for device {DeviceId}", deviceId);

            throw calculationException;
        }
    }
}

