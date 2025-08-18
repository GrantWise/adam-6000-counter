using Industrial.Adam.Oee.Domain.Enums;
using Industrial.Adam.Oee.Domain.Exceptions;
using Industrial.Adam.Oee.Domain.Interfaces;
using Industrial.Adam.Oee.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Domain.Services;

/// <summary>
/// Domain service for quality calculations
/// Handles all quality-related OEE calculations following Single Responsibility Principle
/// </summary>
public interface IQualityCalculationService
{
    /// <summary>
    /// Calculate quality from counter data (good vs. defective pieces)
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="startTime">Period start time</param>
    /// <param name="endTime">Period end time</param>
    /// <param name="productionChannel">Production channel number</param>
    /// <param name="rejectChannel">Reject channel number</param>
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
    /// Calculate first pass yield for a device
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="startTime">Period start time</param>
    /// <param name="endTime">Period end time</param>
    /// <param name="productionChannel">Production channel number</param>
    /// <param name="rejectChannel">Reject channel number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>First pass yield percentage</returns>
    public Task<decimal> CalculateFirstPassYieldAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        int productionChannel = 0,
        int rejectChannel = 1,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate defect rate for a device
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="startTime">Period start time</param>
    /// <param name="endTime">Period end time</param>
    /// <param name="productionChannel">Production channel number</param>
    /// <param name="rejectChannel">Reject channel number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Defect rate percentage</returns>
    public Task<decimal> CalculateDefectRateAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        int productionChannel = 0,
        int rejectChannel = 1,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get quality metrics breakdown
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="startTime">Period start time</param>
    /// <param name="endTime">Period end time</param>
    /// <param name="productionChannel">Production channel number</param>
    /// <param name="rejectChannel">Reject channel number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Quality metrics breakdown</returns>
    public Task<QualityMetrics> GetQualityMetricsAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        int productionChannel = 0,
        int rejectChannel = 1,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of quality calculation service
/// </summary>
public sealed class QualityCalculationService : IQualityCalculationService
{
    private readonly ICounterDataRepository _counterDataRepository;
    private readonly ILogger<QualityCalculationService> _logger;

    /// <summary>
    /// Initialize quality calculation service
    /// </summary>
    /// <param name="counterDataRepository">Counter data repository</param>
    /// <param name="logger">Logger instance</param>
    public QualityCalculationService(
        ICounterDataRepository counterDataRepository,
        ILogger<QualityCalculationService> logger)
    {
        _counterDataRepository = counterDataRepository ?? throw new ArgumentNullException(nameof(counterDataRepository));
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
            "Calculating quality for device {DeviceId} from {StartTime} to {EndTime} (prod: {ProdChannel}, reject: {RejectChannel})",
            deviceId, startTime, endTime, productionChannel, rejectChannel);

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
    public async Task<decimal> CalculateFirstPassYieldAsync(
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
            "Calculating first pass yield for device {DeviceId} from {StartTime} to {EndTime}",
            deviceId, startTime, endTime);

        try
        {
            var quality = await CalculateAsync(
                deviceId, startTime, endTime, productionChannel, rejectChannel, cancellationToken);

            var firstPassYield = quality.Percentage;

            _logger.LogDebug(
                "Calculated first pass yield for device {DeviceId}: {FirstPassYield:F1}%",
                deviceId, firstPassYield);

            return firstPassYield;
        }
        catch (Exception ex) when (!(ex is OeeCalculationException))
        {
            var calculationException = new OeeCalculationException(
                $"Failed to calculate first pass yield for device {deviceId}",
                "FirstPassYield",
                OeeErrorCode.QualityCalculationFailed,
                deviceId,
                startTime,
                endTime,
                ex);

            _logger.LogError(calculationException,
                "First pass yield calculation failed for device {DeviceId}", deviceId);

            throw calculationException;
        }
    }

    /// <inheritdoc />
    public async Task<decimal> CalculateDefectRateAsync(
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
            "Calculating defect rate for device {DeviceId} from {StartTime} to {EndTime}",
            deviceId, startTime, endTime);

        try
        {
            var quality = await CalculateAsync(
                deviceId, startTime, endTime, productionChannel, rejectChannel, cancellationToken);

            var defectRate = 100 - quality.Percentage;

            _logger.LogDebug(
                "Calculated defect rate for device {DeviceId}: {DefectRate:F1}%",
                deviceId, defectRate);

            return defectRate;
        }
        catch (Exception ex) when (!(ex is OeeCalculationException))
        {
            var calculationException = new OeeCalculationException(
                $"Failed to calculate defect rate for device {deviceId}",
                "DefectRate",
                OeeErrorCode.QualityCalculationFailed,
                deviceId,
                startTime,
                endTime,
                ex);

            _logger.LogError(calculationException,
                "Defect rate calculation failed for device {DeviceId}", deviceId);

            throw calculationException;
        }
    }

    /// <inheritdoc />
    public async Task<QualityMetrics> GetQualityMetricsAsync(
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
            "Getting quality metrics for device {DeviceId} from {StartTime} to {EndTime}",
            deviceId, startTime, endTime);

        try
        {
            var productionAggregates = await _counterDataRepository.GetAggregatedDataAsync(
                deviceId, productionChannel, startTime, endTime, cancellationToken);

            var rejectAggregates = await _counterDataRepository.GetAggregatedDataAsync(
                deviceId, rejectChannel, startTime, endTime, cancellationToken);

            var goodPieces = productionAggregates?.TotalCount ?? 0;
            var defectivePieces = rejectAggregates?.TotalCount ?? 0;
            var totalPieces = goodPieces + defectivePieces;

            var qualityPercentage = totalPieces > 0 ? (goodPieces / totalPieces) * 100 : 100;
            var defectRate = totalPieces > 0 ? (defectivePieces / totalPieces) * 100 : 0;

            var metrics = new QualityMetrics(
                QualityPercentage: qualityPercentage,
                DefectRate: defectRate,
                FirstPassYield: qualityPercentage,
                GoodPieces: goodPieces,
                DefectivePieces: defectivePieces,
                TotalPieces: totalPieces,
                DefectsPerMillion: totalPieces > 0 ? (defectivePieces / totalPieces) * 1_000_000 : 0);

            _logger.LogInformation(
                "Quality metrics for device {DeviceId}: Quality={Quality:F1}%, DefectRate={DefectRate:F1}%, Good={Good}, Defects={Defects}",
                deviceId, qualityPercentage, defectRate, goodPieces, defectivePieces);

            return metrics;
        }
        catch (Exception ex) when (!(ex is OeeCalculationException))
        {
            var calculationException = new OeeCalculationException(
                $"Failed to get quality metrics for device {deviceId}",
                "QualityMetrics",
                OeeErrorCode.QualityCalculationFailed,
                deviceId,
                startTime,
                endTime,
                ex);

            _logger.LogError(calculationException,
                "Quality metrics calculation failed for device {DeviceId}", deviceId);

            throw calculationException;
        }
    }
}

/// <summary>
/// Comprehensive quality metrics
/// </summary>
public sealed record QualityMetrics(
    decimal QualityPercentage,
    decimal DefectRate,
    decimal FirstPassYield,
    decimal GoodPieces,
    decimal DefectivePieces,
    decimal TotalPieces,
    decimal DefectsPerMillion)
{
    /// <summary>
    /// Whether quality meets acceptable standards (>95%)
    /// </summary>
    public bool MeetsStandards => QualityPercentage >= 95.0m;

    /// <summary>
    /// Quality grade based on percentage
    /// </summary>
    public string QualityGrade => QualityPercentage switch
    {
        >= 99.0m => "Excellent",
        >= 95.0m => "Good",
        >= 90.0m => "Fair",
        >= 80.0m => "Poor",
        _ => "Unacceptable"
    };
}
