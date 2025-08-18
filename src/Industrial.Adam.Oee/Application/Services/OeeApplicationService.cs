using Industrial.Adam.Oee.Application.DTOs;
using Industrial.Adam.Oee.Application.Interfaces;
using Industrial.Adam.Oee.Domain.Entities;
using Industrial.Adam.Oee.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Application.Services;

/// <summary>
/// Application service for OEE-related business operations
/// </summary>
public class OeeApplicationService : IOeeApplicationService
{
    private readonly IOeeCalculationService _oeeCalculationService;
    private readonly IWorkOrderRepository _workOrderRepository;
    private readonly ICounterDataRepository _counterDataRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<OeeApplicationService> _logger;

    private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(5);
    private const string OeeCacheKeyPrefix = "oee_current_";
    private const string WorkOrderCacheKeyPrefix = "workorder_active_";

    public OeeApplicationService(
        IOeeCalculationService oeeCalculationService,
        IWorkOrderRepository workOrderRepository,
        ICounterDataRepository counterDataRepository,
        IMemoryCache cache,
        ILogger<OeeApplicationService> logger)
    {
        _oeeCalculationService = oeeCalculationService;
        _workOrderRepository = workOrderRepository;
        _counterDataRepository = counterDataRepository;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Get enhanced OEE metrics with additional context
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="includeWorkOrderContext">Whether to include work order context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Enhanced OEE metrics</returns>
    public async Task<OeeMetricsDto> GetEnhancedOeeMetricsAsync(
        string deviceId,
        bool includeWorkOrderContext = true,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting enhanced OEE metrics for device {DeviceId}", deviceId);

        try
        {
            // Check cache first for current OEE
            var cacheKey = $"{OeeCacheKeyPrefix}{deviceId}";

            if (!_cache.TryGetValue(cacheKey, out OeeMetricsDto? cachedMetrics))
            {
                // Calculate current OEE
                var oeeCalculation = await _oeeCalculationService.CalculateCurrentOeeAsync(deviceId, cancellationToken);

                // Get current production rate
                var currentRate = await _counterDataRepository.GetCurrentRateAsync(deviceId, 0, 5, cancellationToken);

                // Initialize metrics DTO
                cachedMetrics = new OeeMetricsDto
                {
                    DeviceId = deviceId,
                    AvailabilityPercent = oeeCalculation.AvailabilityPercentage,
                    PerformancePercent = oeeCalculation.PerformancePercentage,
                    QualityPercent = oeeCalculation.QualityPercentage,
                    OeePercent = oeeCalculation.OeePercentage,
                    CurrentRate = currentRate,
                    WorstFactor = oeeCalculation.GetWorstFactor().ToString(),
                    RequiresAttention = oeeCalculation.RequiresAttention(),
                    PeriodStart = oeeCalculation.CalculationPeriodStart,
                    PeriodEnd = oeeCalculation.CalculationPeriodEnd,
                    LastUpdate = DateTime.UtcNow,
                    Status = "Active" // Default status
                };

                // Add work order context if requested
                if (includeWorkOrderContext)
                {
                    await EnrichWithWorkOrderContextAsync(cachedMetrics, deviceId, cancellationToken);
                }

                // Cache the result
                _cache.Set(cacheKey, cachedMetrics, CacheExpiry);
            }

            return cachedMetrics!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting enhanced OEE metrics for device {DeviceId}", deviceId);
            throw;
        }
    }

    /// <summary>
    /// Get device status with production context
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Device status information</returns>
    public async Task<DeviceStatusDto> GetDeviceStatusAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting device status for {DeviceId}", deviceId);

        try
        {
            // Get active work order
            var activeWorkOrder = await _workOrderRepository.GetActiveByDeviceAsync(deviceId, cancellationToken);

            // Get current production rate
            var currentRate = await _counterDataRepository.GetCurrentRateAsync(deviceId, 0, 5, cancellationToken);

            // Check for current stoppage
            var currentStoppage = await _oeeCalculationService.DetectCurrentStoppageAsync(deviceId, 5, cancellationToken);

            // Determine status
            var status = DetermineDeviceStatus(activeWorkOrder, currentRate, currentStoppage);

            return new DeviceStatusDto
            {
                DeviceId = deviceId,
                Status = status,
                CurrentRate = currentRate,
                HasActiveWorkOrder = activeWorkOrder != null,
                ActiveWorkOrderId = activeWorkOrder?.Id,
                IsInStoppage = currentStoppage?.IsActive == true,
                StoppageDurationMinutes = currentStoppage?.DurationMinutes,
                LastUpdate = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting device status for {DeviceId}", deviceId);
            throw;
        }
    }

    /// <summary>
    /// Validate work order start conditions
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    public async Task<WorkOrderValidationResult> ValidateWorkOrderStartConditionsAsync(
        string deviceId,
        string workOrderId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating work order start conditions for {WorkOrderId} on device {DeviceId}",
            workOrderId, deviceId);

        var issues = new List<string>();
        var warnings = new List<string>();

        try
        {
            // Check if work order already exists
            var existingWorkOrder = await _workOrderRepository.GetByIdAsync(workOrderId, cancellationToken);
            if (existingWorkOrder != null)
            {
                issues.Add($"Work order {workOrderId} already exists");
            }

            // Check if device has active work order
            var activeWorkOrder = await _workOrderRepository.GetActiveByDeviceAsync(deviceId, cancellationToken);
            if (activeWorkOrder != null)
            {
                issues.Add($"Device {deviceId} already has an active work order: {activeWorkOrder.Id}");
            }

            // Check if device has recent production activity
            var hasRecentActivity = await _counterDataRepository.HasProductionActivityAsync(
                deviceId, 0, DateTime.UtcNow.AddMinutes(-30), DateTime.UtcNow, cancellationToken);

            if (hasRecentActivity)
            {
                warnings.Add("Device has recent production activity. Ensure counter readings are accurate.");
            }

            // Check data quality
            var dataValidation = await _oeeCalculationService.ValidateDataSufficiencyAsync(
                deviceId, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow, 5, cancellationToken);

            if (!dataValidation.IsValid)
            {
                warnings.AddRange(dataValidation.Issues);
            }

            return new WorkOrderValidationResult
            {
                IsValid = !issues.Any(),
                Issues = issues,
                Warnings = warnings,
                CanProceed = !issues.Any(),
                ValidationTimestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating work order start conditions");

            return new WorkOrderValidationResult
            {
                IsValid = false,
                Issues = new[] { "Validation failed due to system error" },
                Warnings = Array.Empty<string>(),
                CanProceed = false,
                ValidationTimestamp = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Enrich metrics with work order context
    /// </summary>
    /// <param name="metrics">Metrics to enrich</param>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task EnrichWithWorkOrderContextAsync(
        OeeMetricsDto metrics,
        string deviceId,
        CancellationToken cancellationToken)
    {
        try
        {
            var activeWorkOrder = await _workOrderRepository.GetActiveByDeviceAsync(deviceId, cancellationToken);

            if (activeWorkOrder != null)
            {
                metrics.JobNumber = activeWorkOrder.Id;
                metrics.TargetRate = CalculateTargetRate(activeWorkOrder);
                metrics.Status = DetermineProductionStatus(activeWorkOrder, metrics.CurrentRate);
            }
            else
            {
                metrics.Status = "No Active Job";
                metrics.TargetRate = 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error enriching metrics with work order context for device {DeviceId}", deviceId);
        }
    }

    /// <summary>
    /// Calculate target rate from work order
    /// </summary>
    /// <param name="workOrder">Work order</param>
    /// <returns>Target rate per minute</returns>
    private static decimal CalculateTargetRate(WorkOrder workOrder)
    {
        if (workOrder.ActualStartTime == null)
            return 0;

        var scheduledDuration = (workOrder.ScheduledEndTime - workOrder.ScheduledStartTime).TotalMinutes;
        return scheduledDuration > 0 ? workOrder.PlannedQuantity / (decimal)scheduledDuration : 0;
    }

    /// <summary>
    /// Determine production status based on work order and current rate
    /// </summary>
    /// <param name="workOrder">Work order</param>
    /// <param name="currentRate">Current production rate</param>
    /// <returns>Production status</returns>
    private static string DetermineProductionStatus(WorkOrder workOrder, decimal currentRate)
    {
        if (!workOrder.IsActive)
            return workOrder.Status.ToString();

        if (currentRate == 0)
            return "Stopped";

        if (workOrder.IsBehindSchedule())
            return "Behind Schedule";

        if (workOrder.RequiresAttention())
            return "Attention Required";

        return "Running";
    }

    /// <summary>
    /// Determine device status
    /// </summary>
    /// <param name="activeWorkOrder">Active work order</param>
    /// <param name="currentRate">Current production rate</param>
    /// <param name="currentStoppage">Current stoppage information</param>
    /// <returns>Device status</returns>
    private static string DetermineDeviceStatus(
        WorkOrder? activeWorkOrder,
        decimal currentRate,
        StoppageInfo? currentStoppage)
    {
        if (activeWorkOrder == null)
            return "Idle";

        if (currentStoppage?.IsActive == true)
            return "Stopped";

        if (currentRate == 0)
            return "Not Producing";

        return "Producing";
    }
}

