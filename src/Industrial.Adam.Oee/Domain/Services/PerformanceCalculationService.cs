using Industrial.Adam.Oee.Domain.Entities;
using Industrial.Adam.Oee.Domain.Enums;
using Industrial.Adam.Oee.Domain.Exceptions;
using Industrial.Adam.Oee.Domain.Interfaces;
using Industrial.Adam.Oee.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Domain.Services;

/// <summary>
/// Implementation of performance calculation service
/// </summary>
public sealed class PerformanceCalculationService : IPerformanceCalculationService
{
    private readonly ICounterDataRepository _counterDataRepository;
    private readonly IWorkOrderRepository _workOrderRepository;
    private readonly ILogger<PerformanceCalculationService> _logger;

    /// <summary>
    /// Initialize performance calculation service
    /// </summary>
    /// <param name="counterDataRepository">Counter data repository</param>
    /// <param name="workOrderRepository">Work order repository</param>
    /// <param name="logger">Logger instance</param>
    public PerformanceCalculationService(
        ICounterDataRepository counterDataRepository,
        IWorkOrderRepository workOrderRepository,
        ILogger<PerformanceCalculationService> logger)
    {
        _counterDataRepository = counterDataRepository ?? throw new ArgumentNullException(nameof(counterDataRepository));
        _workOrderRepository = workOrderRepository ?? throw new ArgumentNullException(nameof(workOrderRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Performance> CalculateAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        decimal targetRatePerMinute,
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
                deviceId, 0, startTime, endTime, cancellationToken);

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
    public async Task<Performance> CalculateForWorkOrderAsync(
        string workOrderId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workOrderId))
            throw new ArgumentException("Work order ID cannot be null or empty", nameof(workOrderId));

        _logger.LogDebug("Calculating performance for work order {WorkOrderId}", workOrderId);

        try
        {
            var workOrder = await _workOrderRepository.GetByIdAsync(workOrderId, cancellationToken);
            if (workOrder == null)
            {
                throw new OeeCalculationException(
                    $"Work order {workOrderId} not found",
                    "WorkOrderPerformance",
                    OeeErrorCode.WorkOrderNotFound,
                    null,
                    null,
                    null);
            }

            var startTime = workOrder.ActualStartTime ?? workOrder.ScheduledStartTime;
            var endTime = workOrder.ActualEndTime ?? DateTime.UtcNow;

            // Use work order target rate or default
            var targetRate = workOrder.PlannedQuantity > 0 && (endTime - startTime).TotalMinutes > 0
                ? workOrder.PlannedQuantity / (decimal)(endTime - startTime).TotalMinutes
                : 60m; // Default rate

            return await CalculateAsync(workOrder.ResourceReference, startTime, endTime, targetRate, cancellationToken);
        }
        catch (Exception ex) when (!(ex is OeeCalculationException))
        {
            var calculationException = new OeeCalculationException(
                $"Failed to calculate performance for work order {workOrderId}",
                "WorkOrderPerformance",
                OeeErrorCode.PerformanceCalculationFailed,
                null,
                null,
                null,
                ex);

            _logger.LogError(calculationException,
                "Work order performance calculation failed for {WorkOrderId}", workOrderId);

            throw calculationException;
        }
    }

    /// <inheritdoc />
    public async Task<Performance> CalculateWithConfigurationAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        PerformanceConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Device ID cannot be null or empty", nameof(deviceId));

        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        _logger.LogDebug(
            "Calculating performance with configuration for device {DeviceId} from {StartTime} to {EndTime}",
            deviceId, startTime, endTime);

        try
        {
            var aggregates = await _counterDataRepository.GetAggregatedDataAsync(
                deviceId, configuration.ProductionChannel, startTime, endTime, cancellationToken);

            if (aggregates == null)
            {
                _logger.LogWarning(
                    "No aggregated data available for device {DeviceId}, returning zero performance",
                    deviceId);

                var runTimeMinutes = (decimal)(endTime - startTime).TotalMinutes;
                return new Performance(0, runTimeMinutes, configuration.TargetRatePerMinute);
            }

            var actualRatePerMinute = aggregates.AverageRate * 60;

            // Apply weighted averaging if configured
            if (configuration.UseWeightedAveraging && aggregates.RunTimeMinutes > 0)
            {
                actualRatePerMinute = aggregates.TotalCount / aggregates.RunTimeMinutes;
            }

            var performance = new Performance(
                totalPiecesProduced: aggregates.TotalCount,
                runTimeMinutes: aggregates.RunTimeMinutes,
                targetRatePerMinute: configuration.TargetRatePerMinute,
                actualRatePerMinute: actualRatePerMinute);

            return performance;
        }
        catch (Exception ex) when (!(ex is OeeCalculationException))
        {
            var calculationException = new OeeCalculationException(
                $"Failed to calculate performance with configuration for device {deviceId}",
                "ConfiguredPerformance",
                OeeErrorCode.PerformanceCalculationFailed,
                deviceId,
                startTime,
                endTime,
                ex);

            _logger.LogError(calculationException,
                "Configured performance calculation failed for device {DeviceId}", deviceId);

            throw calculationException;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PerformanceTrend>> GetTrendsAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        int intervalMinutes,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Device ID cannot be null or empty", nameof(deviceId));

        if (intervalMinutes <= 0)
            throw new ArgumentException("Interval must be positive", nameof(intervalMinutes));

        _logger.LogDebug(
            "Getting performance trends for device {DeviceId} from {StartTime} to {EndTime} with {Interval}min intervals",
            deviceId, startTime, endTime, intervalMinutes);

        try
        {
            var trends = new List<PerformanceTrend>();
            var currentTime = startTime;
            var intervalSpan = TimeSpan.FromMinutes(intervalMinutes);
            const decimal defaultTargetRate = 60m; // Default rate for trends

            while (currentTime < endTime)
            {
                var intervalEnd = currentTime.Add(intervalSpan);
                if (intervalEnd > endTime)
                    intervalEnd = endTime;

                var performance = await CalculateAsync(
                    deviceId, currentTime, intervalEnd, defaultTargetRate, cancellationToken);

                trends.Add(new PerformanceTrend(currentTime, performance));

                currentTime = intervalEnd;
            }

            return trends;
        }
        catch (Exception ex) when (!(ex is OeeCalculationException))
        {
            var calculationException = new OeeCalculationException(
                $"Failed to get performance trends for device {deviceId}",
                "PerformanceTrends",
                OeeErrorCode.PerformanceCalculationFailed,
                deviceId,
                startTime,
                endTime,
                ex);

            _logger.LogError(calculationException,
                "Performance trends calculation failed for device {DeviceId}", deviceId);

            throw calculationException;
        }
    }
}
