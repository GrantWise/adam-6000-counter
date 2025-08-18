using Industrial.Adam.Oee.Domain.Enums;
using Industrial.Adam.Oee.Domain.Exceptions;
using Industrial.Adam.Oee.Domain.Interfaces;
using Industrial.Adam.Oee.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Domain.Services;

/// <summary>
/// Domain service for performance calculations
/// Handles all performance-related OEE calculations following Single Responsibility Principle
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
    /// <param name="productionChannel">Production channel number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Performance calculation</returns>
    public Task<Performance> CalculateAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        decimal targetRatePerMinute,
        int productionChannel = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate speed loss for a device in a time period
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="startTime">Period start time</param>
    /// <param name="endTime">Period end time</param>
    /// <param name="targetRatePerMinute">Target production rate per minute</param>
    /// <param name="productionChannel">Production channel number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Speed loss percentage</returns>
    public Task<decimal> CalculateSpeedLossAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        decimal targetRatePerMinute,
        int productionChannel = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate actual production rate from counter data
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="startTime">Period start time</param>
    /// <param name="endTime">Period end time</param>
    /// <param name="productionChannel">Production channel number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Actual production rate per minute</returns>
    public Task<decimal> CalculateActualRateAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        int productionChannel = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate total pieces produced in a time period
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="startTime">Period start time</param>
    /// <param name="endTime">Period end time</param>
    /// <param name="productionChannel">Production channel number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total pieces produced</returns>
    public Task<decimal> CalculateTotalPiecesAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        int productionChannel = 0,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of performance calculation service
/// </summary>
public sealed class PerformanceCalculationService : IPerformanceCalculationService
{
    private readonly ICounterDataRepository _counterDataRepository;
    private readonly ILogger<PerformanceCalculationService> _logger;

    /// <summary>
    /// Initialize performance calculation service
    /// </summary>
    /// <param name="counterDataRepository">Counter data repository</param>
    /// <param name="logger">Logger instance</param>
    public PerformanceCalculationService(
        ICounterDataRepository counterDataRepository,
        ILogger<PerformanceCalculationService> logger)
    {
        _counterDataRepository = counterDataRepository ?? throw new ArgumentNullException(nameof(counterDataRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Performance> CalculateAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        decimal targetRatePerMinute,
        int productionChannel = 0,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Device ID cannot be null or empty", nameof(deviceId));

        if (endTime <= startTime)
            throw new ArgumentException("End time must be after start time", nameof(endTime));

        if (targetRatePerMinute <= 0)
            throw new ArgumentException("Target rate must be positive", nameof(targetRatePerMinute));

        _logger.LogDebug(
            "Calculating performance for device {DeviceId} from {StartTime} to {EndTime} (target: {TargetRate}/min)",
            deviceId, startTime, endTime, targetRatePerMinute);

        try
        {
            var aggregates = await _counterDataRepository.GetAggregatedDataAsync(
                deviceId, productionChannel, startTime, endTime, cancellationToken);

            if (aggregates == null)
            {
                _logger.LogWarning(
                    "No aggregated data available for device {DeviceId}, returning zero performance",
                    deviceId);

                var runTimeMinutes = (decimal)(endTime - startTime).TotalMinutes;
                return new Performance(0, runTimeMinutes, targetRatePerMinute);
            }

            // Convert rate from per-second to per-minute
            var actualRatePerMinute = aggregates.AverageRate * 60;

            var performance = new Performance(
                totalPiecesProduced: aggregates.TotalCount,
                runTimeMinutes: aggregates.RunTimeMinutes,
                targetRatePerMinute: targetRatePerMinute,
                actualRatePerMinute: actualRatePerMinute);

            _logger.LogInformation(
                "Calculated performance for device {DeviceId}: {PerformancePercentage:F1}% ({ActualRate:F1}/{TargetRate:F1} pieces/min)",
                deviceId, performance.Percentage, actualRatePerMinute, targetRatePerMinute);

            return performance;
        }
        catch (Exception ex) when (!(ex is OeeCalculationException))
        {
            var calculationException = new OeeCalculationException(
                $"Failed to calculate performance for device {deviceId}",
                "Performance",
                OeeErrorCode.PerformanceCalculationFailed,
                deviceId,
                startTime,
                endTime,
                ex);

            _logger.LogError(calculationException,
                "Performance calculation failed for device {DeviceId}", deviceId);

            throw calculationException;
        }
    }

    /// <inheritdoc />
    public async Task<decimal> CalculateSpeedLossAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        decimal targetRatePerMinute,
        int productionChannel = 0,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Device ID cannot be null or empty", nameof(deviceId));

        if (endTime <= startTime)
            throw new ArgumentException("End time must be after start time", nameof(endTime));

        if (targetRatePerMinute <= 0)
            throw new ArgumentException("Target rate must be positive", nameof(targetRatePerMinute));

        _logger.LogDebug(
            "Calculating speed loss for device {DeviceId} from {StartTime} to {EndTime}",
            deviceId, startTime, endTime);

        try
        {
            var actualRate = await CalculateActualRateAsync(
                deviceId, startTime, endTime, productionChannel, cancellationToken);

            if (targetRatePerMinute == 0)
            {
                return 0; // No target rate defined
            }

            var speedLoss = Math.Max(0, (targetRatePerMinute - actualRate) / targetRatePerMinute * 100);

            _logger.LogDebug(
                "Calculated speed loss for device {DeviceId}: {SpeedLoss:F1}% ({ActualRate:F1} vs {TargetRate:F1})",
                deviceId, speedLoss, actualRate, targetRatePerMinute);

            return speedLoss;
        }
        catch (Exception ex) when (!(ex is OeeCalculationException))
        {
            var calculationException = new OeeCalculationException(
                $"Failed to calculate speed loss for device {deviceId}",
                "SpeedLoss",
                OeeErrorCode.PerformanceCalculationFailed,
                deviceId,
                startTime,
                endTime,
                ex);

            _logger.LogError(calculationException,
                "Speed loss calculation failed for device {DeviceId}", deviceId);

            throw calculationException;
        }
    }

    /// <inheritdoc />
    public async Task<decimal> CalculateActualRateAsync(
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
            "Calculating actual rate for device {DeviceId} channel {Channel} from {StartTime} to {EndTime}",
            deviceId, productionChannel, startTime, endTime);

        try
        {
            var aggregates = await _counterDataRepository.GetAggregatedDataAsync(
                deviceId, productionChannel, startTime, endTime, cancellationToken);

            var actualRatePerMinute = (aggregates?.AverageRate ?? 0) * 60; // Convert to per-minute

            _logger.LogDebug(
                "Calculated actual rate for device {DeviceId}: {ActualRate:F1} pieces/minute",
                deviceId, actualRatePerMinute);

            return actualRatePerMinute;
        }
        catch (Exception ex) when (!(ex is OeeCalculationException))
        {
            var calculationException = new OeeCalculationException(
                $"Failed to calculate actual rate for device {deviceId}",
                "ActualRate",
                OeeErrorCode.PerformanceCalculationFailed,
                deviceId,
                startTime,
                endTime,
                ex);

            _logger.LogError(calculationException,
                "Actual rate calculation failed for device {DeviceId}", deviceId);

            throw calculationException;
        }
    }

    /// <inheritdoc />
    public async Task<decimal> CalculateTotalPiecesAsync(
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
            "Calculating total pieces for device {DeviceId} channel {Channel} from {StartTime} to {EndTime}",
            deviceId, productionChannel, startTime, endTime);

        try
        {
            var aggregates = await _counterDataRepository.GetAggregatedDataAsync(
                deviceId, productionChannel, startTime, endTime, cancellationToken);

            var totalPieces = aggregates?.TotalCount ?? 0;

            _logger.LogDebug(
                "Calculated total pieces for device {DeviceId}: {TotalPieces} pieces",
                deviceId, totalPieces);

            return totalPieces;
        }
        catch (Exception ex) when (!(ex is OeeCalculationException))
        {
            var calculationException = new OeeCalculationException(
                $"Failed to calculate total pieces for device {deviceId}",
                "TotalPieces",
                OeeErrorCode.PerformanceCalculationFailed,
                deviceId,
                startTime,
                endTime,
                ex);

            _logger.LogError(calculationException,
                "Total pieces calculation failed for device {DeviceId}", deviceId);

            throw calculationException;
        }
    }
}
