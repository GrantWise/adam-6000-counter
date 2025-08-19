using Industrial.Adam.Oee.Domain.Entities;
using Industrial.Adam.Oee.Domain.Enums;
using Industrial.Adam.Oee.Domain.Exceptions;
using Industrial.Adam.Oee.Domain.Interfaces;
using Industrial.Adam.Oee.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Domain.Services;

/// <summary>
/// Implementation of quality calculation service
/// </summary>
public sealed class QualityCalculationService : IQualityCalculationService
{
    private readonly ICounterDataRepository _counterDataRepository;
    private readonly IWorkOrderRepository _workOrderRepository;
    private readonly ILogger<QualityCalculationService> _logger;

    /// <summary>
    /// Initialize quality calculation service
    /// </summary>
    /// <param name="counterDataRepository">Counter data repository</param>
    /// <param name="workOrderRepository">Work order repository</param>
    /// <param name="logger">Logger instance</param>
    public QualityCalculationService(
        ICounterDataRepository counterDataRepository,
        IWorkOrderRepository workOrderRepository,
        ILogger<QualityCalculationService> logger)
    {
        _counterDataRepository = counterDataRepository ?? throw new ArgumentNullException(nameof(counterDataRepository));
        _workOrderRepository = workOrderRepository ?? throw new ArgumentNullException(nameof(workOrderRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Quality> CalculateAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        int productionChannel = 0,
        int rejectChannel = 1,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Device ID cannot be null or empty", nameof(deviceId));

        if (endTime <= startTime)
            throw new ArgumentException("End time must be after start time", nameof(endTime));

        _logger.LogDebug(
            "Calculating quality for device {DeviceId} from {StartTime} to {EndTime}",
            deviceId, startTime, endTime);

        try
        {
            var productionAggregates = await _counterDataRepository.GetAggregatedDataAsync(
                deviceId, productionChannel, startTime, endTime, cancellationToken);

            var rejectAggregates = await _counterDataRepository.GetAggregatedDataAsync(
                deviceId, rejectChannel, startTime, endTime, cancellationToken);

            var goodPieces = productionAggregates?.TotalCount ?? 0;
            var defectivePieces = rejectAggregates?.TotalCount ?? 0;

            var quality = Quality.FromCounterChannels(goodPieces, defectivePieces);

            _logger.LogInformation(
                "Calculated quality for device {DeviceId}: {QualityPercentage:F1}% ({GoodPieces}/{TotalPieces} pieces)",
                deviceId, quality.Percentage, goodPieces, goodPieces + defectivePieces);

            return quality;
        }
        catch (Exception ex) when (!(ex is OeeCalculationException))
        {
            var calculationException = new OeeCalculationException(
                $"Failed to calculate quality for device {deviceId}",
                "Quality",
                OeeErrorCode.QualityCalculationFailed,
                deviceId,
                startTime,
                endTime,
                ex);

            _logger.LogError(calculationException,
                "Quality calculation failed for device {DeviceId}", deviceId);

            throw calculationException;
        }
    }

    /// <inheritdoc />
    public async Task<Quality> CalculateForWorkOrderAsync(
        string workOrderId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workOrderId))
            throw new ArgumentException("Work order ID cannot be null or empty", nameof(workOrderId));

        _logger.LogDebug("Calculating quality for work order {WorkOrderId}", workOrderId);

        try
        {
            var workOrder = await _workOrderRepository.GetByIdAsync(workOrderId, cancellationToken);
            if (workOrder == null)
            {
                throw new OeeCalculationException(
                    $"Work order {workOrderId} not found",
                    "WorkOrderQuality",
                    OeeErrorCode.WorkOrderNotFound,
                    null,
                    null,
                    null);
            }

            // Use work order actual quantities if available
            if (workOrder.ActualQuantityGood > 0 || workOrder.ActualQuantityScrap > 0)
            {
                return new Quality(workOrder.ActualQuantityGood, workOrder.ActualQuantityScrap);
            }

            // Fallback to counter data calculation
            var startTime = workOrder.ActualStartTime ?? workOrder.ScheduledStartTime;
            var endTime = workOrder.ActualEndTime ?? DateTime.UtcNow;

            return await CalculateAsync(workOrder.ResourceReference, startTime, endTime, 0, 1, cancellationToken);
        }
        catch (Exception ex) when (!(ex is OeeCalculationException))
        {
            var calculationException = new OeeCalculationException(
                $"Failed to calculate quality for work order {workOrderId}",
                "WorkOrderQuality",
                OeeErrorCode.QualityCalculationFailed,
                null,
                null,
                null,
                ex);

            _logger.LogError(calculationException,
                "Work order quality calculation failed for {WorkOrderId}", workOrderId);

            throw calculationException;
        }
    }

    /// <inheritdoc />
    public async Task<Quality> CalculateWithConfigurationAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        QualityConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Device ID cannot be null or empty", nameof(deviceId));

        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        _logger.LogDebug(
            "Calculating quality with configuration for device {DeviceId} from {StartTime} to {EndTime}",
            deviceId, startTime, endTime);

        try
        {
            var quality = await CalculateAsync(
                deviceId, startTime, endTime,
                configuration.ProductionChannel, configuration.RejectChannel,
                cancellationToken);

            // Apply quality gates if configured
            if (configuration.QualityGates?.Any() == true)
            {
                foreach (var gate in configuration.QualityGates)
                {
                    if (quality.Percentage < gate.Threshold)
                    {
                        _logger.LogWarning(
                            "Quality gate '{GateName}' breached for device {DeviceId}: {Quality:F1}% < {Threshold:F1}% (Alert: {AlertLevel})",
                            gate.Name, deviceId, quality.Percentage, gate.Threshold, gate.AlertLevel);
                    }
                }
            }

            return quality;
        }
        catch (Exception ex) when (!(ex is OeeCalculationException))
        {
            var calculationException = new OeeCalculationException(
                $"Failed to calculate quality with configuration for device {deviceId}",
                "ConfiguredQuality",
                OeeErrorCode.QualityCalculationFailed,
                deviceId,
                startTime,
                endTime,
                ex);

            _logger.LogError(calculationException,
                "Configured quality calculation failed for device {DeviceId}", deviceId);

            throw calculationException;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<QualityTrend>> GetTrendsAsync(
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
            "Getting quality trends for device {DeviceId} from {StartTime} to {EndTime} with {Interval}min intervals",
            deviceId, startTime, endTime, intervalMinutes);

        try
        {
            var trends = new List<QualityTrend>();
            var currentTime = startTime;
            var intervalSpan = TimeSpan.FromMinutes(intervalMinutes);

            while (currentTime < endTime)
            {
                var intervalEnd = currentTime.Add(intervalSpan);
                if (intervalEnd > endTime)
                    intervalEnd = endTime;

                var quality = await CalculateAsync(
                    deviceId, currentTime, intervalEnd, 0, 1, cancellationToken);

                trends.Add(new QualityTrend(currentTime, quality));

                currentTime = intervalEnd;
            }

            return trends;
        }
        catch (Exception ex) when (!(ex is OeeCalculationException))
        {
            var calculationException = new OeeCalculationException(
                $"Failed to get quality trends for device {deviceId}",
                "QualityTrends",
                OeeErrorCode.QualityCalculationFailed,
                deviceId,
                startTime,
                endTime,
                ex);

            _logger.LogError(calculationException,
                "Quality trends calculation failed for device {DeviceId}", deviceId);

            throw calculationException;
        }
    }
}
