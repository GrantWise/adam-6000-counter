using Industrial.Adam.Oee.Domain.Entities;
using Industrial.Adam.Oee.Domain.Interfaces;
using Industrial.Adam.Oee.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Domain.Services;

/// <summary>
/// Domain service for OEE calculations
/// 
/// Implements Overall Equipment Effectiveness calculations following Domain-Driven Design principles.
/// Encapsulates complex business logic for calculating OEE metrics from counter data and work orders.
/// </summary>
public sealed class OeeCalculationService : IOeeCalculationService
{
    private readonly ICounterDataRepository _counterDataRepository;
    private readonly IWorkOrderRepository _workOrderRepository;
    private readonly ILogger<OeeCalculationService> _logger;

    /// <summary>
    /// Initializes a new instance of the OEE calculation service
    /// </summary>
    /// <param name="counterDataRepository">Counter data repository</param>
    /// <param name="workOrderRepository">Work order repository</param>
    /// <param name="logger">Logger instance</param>
    public OeeCalculationService(
        ICounterDataRepository counterDataRepository,
        IWorkOrderRepository workOrderRepository,
        ILogger<OeeCalculationService> logger)
    {
        _counterDataRepository = counterDataRepository ?? throw new ArgumentNullException(nameof(counterDataRepository));
        _workOrderRepository = workOrderRepository ?? throw new ArgumentNullException(nameof(workOrderRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Calculate current OEE metrics for a device
    /// </summary>
    public async Task<OeeCalculation> CalculateCurrentOeeAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calculating current OEE for device {DeviceId}", deviceId);

        try
        {
            var endTime = DateTime.UtcNow;

            // Get current active work order for context
            var currentWorkOrder = await _workOrderRepository.GetActiveByDeviceAsync(deviceId, cancellationToken);

            if (currentWorkOrder == null)
            {
                _logger.LogWarning("No active work order found for device {DeviceId}, using default calculation period", deviceId);
                return await CreateDefaultOeeCalculationAsync(deviceId, endTime, cancellationToken);
            }

            // Calculate from work order start time or last hour (whichever is more recent)
            var startTime = GetCalculationStartTime(currentWorkOrder);

            return await CalculateOeeForPeriodAsync(deviceId, startTime, endTime, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating current OEE for device {DeviceId}", deviceId);
            return await CreateDefaultOeeCalculationAsync(deviceId, DateTime.UtcNow, cancellationToken);
        }
    }

    /// <summary>
    /// Calculate OEE metrics for a specific time period
    /// </summary>
    public async Task<OeeCalculation> CalculateOeeForPeriodAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calculating OEE for device {DeviceId} from {StartTime} to {EndTime}",
            deviceId, startTime, endTime);

        // Validate data sufficiency
        var validation = await ValidateDataSufficiencyAsync(deviceId, startTime, endTime, 10, cancellationToken);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Insufficient data for OEE calculation: {Issues}", string.Join(", ", validation.Issues));
            return await CreateDefaultOeeCalculationAsync(deviceId, endTime, cancellationToken);
        }

        // Get configuration for the device
        var config = await GetCalculationConfigurationAsync(deviceId, cancellationToken);

        // Calculate individual components
        var availability = await CalculateAvailabilityInternalAsync(deviceId, startTime, endTime, null, cancellationToken);
        var performance = await CalculatePerformanceAsync(deviceId, startTime, endTime, config.DefaultTargetRate, cancellationToken);
        var quality = await CalculateQualityAsync(deviceId, startTime, endTime, config.ProductionChannel, config.RejectChannel, cancellationToken);

        return new OeeCalculation(
            null, // Auto-generate ID
            deviceId,
            startTime,
            endTime,
            availability,
            performance,
            quality
        );
    }

    /// <summary>
    /// Calculate OEE metrics using a specific work order context
    /// </summary>
    public async Task<OeeCalculation> CalculateOeeForWorkOrderAsync(
        WorkOrder workOrder,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calculating OEE for work order {WorkOrderId}", workOrder.Id);

        var startTime = workOrder.ActualStartTime ?? workOrder.ScheduledStartTime;
        var endTime = workOrder.ActualEndTime ?? DateTime.UtcNow;

        // Calculate period duration
        var periodMinutes = (decimal)(endTime - startTime).TotalMinutes;

        // Create availability based on work order schedule and actual runtime
        var availability = new Availability(
            plannedTimeMinutes: periodMinutes,
            actualRunTimeMinutes: CalculateActualRunTimeFromWorkOrder(workOrder, periodMinutes)
        );

        // Calculate performance based on work order targets and actual production
        var performance = await CalculatePerformanceFromWorkOrderAsync(workOrder, periodMinutes, cancellationToken);

        // Calculate quality from work order actual quantities
        var quality = new Quality(
            goodPieces: workOrder.ActualQuantityGood,
            defectivePieces: workOrder.ActualQuantityScrap
        );

        return new OeeCalculation(
            null, // Auto-generate ID
            workOrder.ResourceReference,
            startTime,
            endTime,
            availability,
            performance,
            quality
        );
    }

    /// <summary>
    /// Calculate availability from counter data and downtime records
    /// </summary>
    Task<Availability> IOeeCalculationService.CalculateAvailabilityAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        IEnumerable<Industrial.Adam.Oee.Domain.ValueObjects.DowntimeRecord>? downtimeRecords,
        CancellationToken cancellationToken)
    {
        return CalculateAvailabilityInternalAsync(deviceId, startTime, endTime, downtimeRecords, cancellationToken);
    }

    /// <summary>
    /// Internal implementation of availability calculation
    /// </summary>
    private async Task<Availability> CalculateAvailabilityInternalAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        IEnumerable<Industrial.Adam.Oee.Domain.ValueObjects.DowntimeRecord>? downtimeRecords = null,
        CancellationToken cancellationToken = default)
    {
        var plannedTimeMinutes = (decimal)(endTime - startTime).TotalMinutes;

        if (downtimeRecords != null)
        {
            return Availability.FromDowntimeRecords(plannedTimeMinutes, downtimeRecords);
        }

        // Calculate actual run time from counter data
        var config = await GetCalculationConfigurationAsync(deviceId, cancellationToken);
        var aggregates = await _counterDataRepository.GetAggregatedDataAsync(
            deviceId, config.ProductionChannel, startTime, endTime, cancellationToken);

        var actualRunTimeMinutes = aggregates?.RunTimeMinutes ?? 0;

        return new Availability(plannedTimeMinutes, actualRunTimeMinutes);
    }

    /// <summary>
    /// Calculate performance from counter data and target rates
    /// </summary>
    public async Task<Performance> CalculatePerformanceAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        decimal targetRatePerMinute,
        CancellationToken cancellationToken = default)
    {
        var config = await GetCalculationConfigurationAsync(deviceId, cancellationToken);
        var aggregates = await _counterDataRepository.GetAggregatedDataAsync(
            deviceId, config.ProductionChannel, startTime, endTime, cancellationToken);

        if (aggregates == null)
        {
            var runTimeMinutes = (decimal)(endTime - startTime).TotalMinutes;
            return new Performance(0, runTimeMinutes, targetRatePerMinute);
        }

        return new Performance(
            totalPiecesProduced: aggregates.TotalCount,
            runTimeMinutes: aggregates.RunTimeMinutes,
            targetRatePerMinute: targetRatePerMinute,
            actualRatePerMinute: aggregates.AverageRate * 60 // Convert from per-second to per-minute
        );
    }

    /// <summary>
    /// Calculate quality from counter data (good vs. defective pieces)
    /// </summary>
    public async Task<Quality> CalculateQualityAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        int productionChannel = 0,
        int rejectChannel = 1,
        CancellationToken cancellationToken = default)
    {
        var productionAggregates = await _counterDataRepository.GetAggregatedDataAsync(
            deviceId, productionChannel, startTime, endTime, cancellationToken);

        var rejectAggregates = await _counterDataRepository.GetAggregatedDataAsync(
            deviceId, rejectChannel, startTime, endTime, cancellationToken);

        var goodPieces = productionAggregates?.TotalCount ?? 0;
        var defectivePieces = rejectAggregates?.TotalCount ?? 0;

        return Quality.FromCounterChannels(goodPieces, defectivePieces);
    }

    /// <summary>
    /// Detect current stoppage for a device
    /// </summary>
    public async Task<StoppageInfo?> DetectCurrentStoppageAsync(
        string deviceId,
        int minimumStoppageMinutes = 5,
        CancellationToken cancellationToken = default)
    {
        var config = await GetCalculationConfigurationAsync(deviceId, cancellationToken);
        var endTime = DateTime.UtcNow;
        var startTime = endTime.AddHours(-2); // Look back 2 hours

        var downtimePeriods = await _counterDataRepository.GetDowntimePeriodsAsync(
            deviceId, config.ProductionChannel, startTime, endTime, minimumStoppageMinutes, cancellationToken);

        var currentStoppage = downtimePeriods.FirstOrDefault(d => d.IsOngoing);
        if (currentStoppage == null)
            return null;

        // Calculate estimated impact
        var estimatedImpact = await CalculateStoppageImpactAsync(deviceId, currentStoppage, cancellationToken);

        return new StoppageInfo(
            currentStoppage.StartTime,
            currentStoppage.DurationMinutes,
            currentStoppage.IsOngoing,
            estimatedImpact
        );
    }

    /// <summary>
    /// Validate that sufficient data exists for reliable OEE calculation
    /// </summary>
    public async Task<OeeDataValidationResult> ValidateDataSufficiencyAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        int minimumDataPoints = 10,
        CancellationToken cancellationToken = default)
    {
        var issues = new List<string>();
        var recommendations = new List<string>();

        // Check for sufficient data points
        var counterData = await _counterDataRepository.GetDataForPeriodAsync(deviceId, startTime, endTime, cancellationToken);
        var dataPoints = counterData.Count();

        if (dataPoints < minimumDataPoints)
        {
            issues.Add($"Insufficient data points: {dataPoints} < {minimumDataPoints}");
            recommendations.Add("Ensure device is properly connected and data collection is active");
        }

        // Check time period validity
        var periodHours = (endTime - startTime).TotalHours;
        if (periodHours < 0.1) // Less than 6 minutes
        {
            issues.Add("Time period too short for reliable calculation");
            recommendations.Add("Use a minimum calculation period of 10 minutes");
        }

        if (periodHours > 24)
        {
            recommendations.Add("Consider breaking long periods into smaller segments for better granularity");
        }

        // Check for production activity
        var hasActivity = await _counterDataRepository.HasProductionActivityAsync(deviceId, 0, startTime, endTime, cancellationToken);
        if (!hasActivity)
        {
            issues.Add("No production activity detected in the time period");
            recommendations.Add("Verify equipment is running and producing output");
        }

        return new OeeDataValidationResult(
            IsValid: issues.Count == 0,
            DataPoints: dataPoints,
            MinimumRequired: minimumDataPoints,
            Issues: issues,
            Recommendations: recommendations
        );
    }

    /// <summary>
    /// Get OEE calculation configuration for a device
    /// </summary>
    public async Task<OeeCalculationConfiguration> GetCalculationConfigurationAsync(
        string deviceId,
        CancellationToken cancellationToken = default)
    {
        // For now, return default configuration
        // In future, this could load device-specific configuration from a repository
        await Task.CompletedTask;

        return new OeeCalculationConfiguration(deviceId);
    }

    /// <summary>
    /// Update OEE calculation configuration for a device
    /// </summary>
    public async Task<bool> UpdateCalculationConfigurationAsync(
        string deviceId,
        OeeCalculationConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        // For now, return true (configuration updates not implemented)
        // In future, this would persist configuration to a repository
        await Task.CompletedTask;

        _logger.LogInformation("Configuration update requested for device {DeviceId} (not yet implemented)", deviceId);
        return true;
    }

    /// <summary>
    /// Calculate OEE trends over multiple time periods
    /// </summary>
    public async Task<IEnumerable<OeeCalculation>> CalculateOeeTrendsAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        TimeSpan periodDuration,
        CancellationToken cancellationToken = default)
    {
        var calculations = new List<OeeCalculation>();
        var currentStart = startTime;

        while (currentStart < endTime)
        {
            var currentEnd = currentStart.Add(periodDuration);
            if (currentEnd > endTime)
                currentEnd = endTime;

            try
            {
                var calculation = await CalculateOeeForPeriodAsync(deviceId, currentStart, currentEnd, cancellationToken);
                calculations.Add(calculation);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to calculate OEE for period {Start} to {End}", currentStart, currentEnd);
            }

            currentStart = currentEnd;
        }

        return calculations;
    }

    // Private helper methods

    private static DateTime GetCalculationStartTime(WorkOrder workOrder)
    {
        var workOrderStart = workOrder.ActualStartTime ?? workOrder.ScheduledStartTime;
        var oneHourAgo = DateTime.UtcNow.AddHours(-1);

        // Use work order start time, but not more than 1 hour ago for performance
        return workOrderStart > oneHourAgo ? workOrderStart : oneHourAgo;
    }

    private async Task<OeeCalculation> CreateDefaultOeeCalculationAsync(
        string deviceId,
        DateTime endTime,
        CancellationToken cancellationToken)
    {
        var startTime = endTime.AddHours(-1);
        var config = await GetCalculationConfigurationAsync(deviceId, cancellationToken);

        return new OeeCalculation(
            null,
            deviceId,
            startTime,
            endTime,
            new Availability(60, 0),
            new Performance(0, 0, config.DefaultTargetRate),
            new Quality(0, 0)
        );
    }

    private static decimal CalculateActualRunTimeFromWorkOrder(WorkOrder workOrder, decimal periodMinutes)
    {
        // Simple heuristic: if work order is producing, assume it's running
        // In a real system, this would use actual counter data
        if (workOrder.TotalQuantityProduced > 0)
        {
            // Estimate runtime based on production activity
            var estimatedEfficiency = Math.Min(workOrder.GetCompletionPercentage() / 100m, 1m);
            return periodMinutes * estimatedEfficiency;
        }

        return 0;
    }

    private async Task<Performance> CalculatePerformanceFromWorkOrderAsync(
        WorkOrder workOrder,
        decimal periodMinutes,
        CancellationToken cancellationToken)
    {
        // Get actual counter data for more accurate performance calculation
        var config = await GetCalculationConfigurationAsync(workOrder.ResourceReference, cancellationToken);

        var startTime = workOrder.ActualStartTime ?? workOrder.ScheduledStartTime;
        var endTime = workOrder.ActualEndTime ?? DateTime.UtcNow;

        var aggregates = await _counterDataRepository.GetAggregatedDataAsync(
            workOrder.ResourceReference, config.ProductionChannel, startTime, endTime, cancellationToken);

        if (aggregates != null)
        {
            return new Performance(
                aggregates.TotalCount,
                aggregates.RunTimeMinutes,
                config.DefaultTargetRate,
                aggregates.AverageRate * 60
            );
        }

        // Fallback to work order data
        return new Performance(
            workOrder.TotalQuantityProduced,
            periodMinutes,
            config.DefaultTargetRate
        );
    }

    private async Task<StoppageImpact> CalculateStoppageImpactAsync(
        string deviceId,
        DowntimePeriod stoppage,
        CancellationToken cancellationToken)
    {
        var config = await GetCalculationConfigurationAsync(deviceId, cancellationToken);

        // Calculate lost production based on target rate
        var lostProductionUnits = stoppage.DurationMinutes * config.DefaultTargetRate;

        // Calculate availability impact
        var lookbackHours = Math.Max(stoppage.DurationMinutes / 60m, 1m);
        var totalPeriodMinutes = lookbackHours * 60;
        var availabilityImpact = (stoppage.DurationMinutes / totalPeriodMinutes) * 100;

        return new StoppageImpact(
            LostProductionUnits: lostProductionUnits,
            LostRevenue: null, // Would need product pricing data
            AvailabilityImpact: availabilityImpact
        );
    }
}
