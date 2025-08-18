using Industrial.Adam.Oee.Domain.Entities;
using Industrial.Adam.Oee.Domain.ValueObjects;

namespace Industrial.Adam.Oee.Domain.Interfaces;

/// <summary>
/// Domain service interface for OEE calculations
/// </summary>
public interface IOeeCalculationService
{
    /// <summary>
    /// Calculate current OEE metrics for a device
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current OEE calculation</returns>
    public Task<OeeCalculation> CalculateCurrentOeeAsync(string deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate OEE metrics for a specific time period
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="startTime">Start of calculation period</param>
    /// <param name="endTime">End of calculation period</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>OEE calculation for the specified period</returns>
    public Task<OeeCalculation> CalculateOeeForPeriodAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate OEE metrics using a specific work order context
    /// </summary>
    /// <param name="workOrder">Work order providing production context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>OEE calculation based on work order data</returns>
    public Task<OeeCalculation> CalculateOeeForWorkOrderAsync(
        WorkOrder workOrder,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate availability from counter data and downtime records
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="startTime">Start of calculation period</param>
    /// <param name="endTime">End of calculation period</param>
    /// <param name="downtimeRecords">Optional downtime records</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Availability value object</returns>
    public Task<Availability> CalculateAvailabilityAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        IEnumerable<Industrial.Adam.Oee.Domain.ValueObjects.DowntimeRecord>? downtimeRecords = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate performance from counter data and target rates
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="startTime">Start of calculation period</param>
    /// <param name="endTime">End of calculation period</param>
    /// <param name="targetRatePerMinute">Target production rate per minute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Performance value object</returns>
    public Task<Performance> CalculatePerformanceAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        decimal targetRatePerMinute,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate quality from counter data (good vs. defective pieces)
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="startTime">Start of calculation period</param>
    /// <param name="endTime">End of calculation period</param>
    /// <param name="productionChannel">Channel number for good pieces (default 0)</param>
    /// <param name="rejectChannel">Channel number for defective pieces (default 1)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Quality value object</returns>
    public Task<Quality> CalculateQualityAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        int productionChannel = 0,
        int rejectChannel = 1,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detect current stoppage for a device
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="minimumStoppageMinutes">Minimum duration to be considered a stoppage</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current stoppage information or null if not stopped</returns>
    public Task<StoppageInfo?> DetectCurrentStoppageAsync(
        string deviceId,
        int minimumStoppageMinutes = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate that sufficient data exists for reliable OEE calculation
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="startTime">Start of calculation period</param>
    /// <param name="endTime">End of calculation period</param>
    /// <param name="minimumDataPoints">Minimum required data points</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    public Task<OeeDataValidationResult> ValidateDataSufficiencyAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        int minimumDataPoints = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get OEE calculation configuration for a device
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>OEE calculation configuration</returns>
    public Task<OeeCalculationConfiguration> GetCalculationConfigurationAsync(
        string deviceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update OEE calculation configuration for a device
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="configuration">New configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if configuration was updated successfully</returns>
    public Task<bool> UpdateCalculationConfigurationAsync(
        string deviceId,
        OeeCalculationConfiguration configuration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate OEE trends over multiple time periods
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="startTime">Start of overall time range</param>
    /// <param name="endTime">End of overall time range</param>
    /// <param name="periodDuration">Duration of each time period</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of OEE calculations for each period</returns>
    public Task<IEnumerable<OeeCalculation>> CalculateOeeTrendsAsync(
        string deviceId,
        DateTime startTime,
        DateTime endTime,
        TimeSpan periodDuration,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Information about a current stoppage
/// </summary>
/// <param name="StartTime">When the stoppage started</param>
/// <param name="DurationMinutes">Duration in minutes</param>
/// <param name="IsActive">Whether the stoppage is currently active</param>
/// <param name="EstimatedImpact">Estimated production impact</param>
public record StoppageInfo(
    DateTime StartTime,
    decimal DurationMinutes,
    bool IsActive,
    StoppageImpact? EstimatedImpact = null
);

/// <summary>
/// Estimated impact of a stoppage
/// </summary>
/// <param name="LostProductionUnits">Estimated units of production lost</param>
/// <param name="LostRevenue">Estimated revenue impact</param>
/// <param name="AvailabilityImpact">Impact on availability percentage</param>
public record StoppageImpact(
    decimal LostProductionUnits,
    decimal? LostRevenue,
    decimal AvailabilityImpact
);

/// <summary>
/// Result of OEE data validation
/// </summary>
/// <param name="IsValid">Whether data is sufficient for calculation</param>
/// <param name="DataPoints">Actual number of data points found</param>
/// <param name="MinimumRequired">Minimum data points required</param>
/// <param name="Issues">List of any data quality issues found</param>
/// <param name="Recommendations">Recommendations for improving data quality</param>
public record OeeDataValidationResult(
    bool IsValid,
    int DataPoints,
    int MinimumRequired,
    IEnumerable<string> Issues,
    IEnumerable<string> Recommendations
);

/// <summary>
/// Configuration for OEE calculations
/// </summary>
/// <param name="DeviceId">Device identifier</param>
/// <param name="ProductionChannel">Channel number for production counting</param>
/// <param name="RejectChannel">Channel number for reject counting</param>
/// <param name="DefaultTargetRate">Default target production rate per minute</param>
/// <param name="MinimumDataPoints">Minimum data points required for calculation</param>
/// <param name="StoppageThresholdMinutes">Minimum minutes of zero production to be considered a stoppage</param>
/// <param name="QualityThreshold">Quality threshold for attention alerts</param>
/// <param name="PerformanceThreshold">Performance threshold for attention alerts</param>
/// <param name="AvailabilityThreshold">Availability threshold for attention alerts</param>
/// <param name="DataRetentionDays">Number of days to retain calculation data</param>
public record OeeCalculationConfiguration(
    string DeviceId,
    int ProductionChannel = 0,
    int RejectChannel = 1,
    decimal DefaultTargetRate = 60m,
    int MinimumDataPoints = 10,
    int StoppageThresholdMinutes = 5,
    decimal QualityThreshold = 95m,
    decimal PerformanceThreshold = 75m,
    decimal AvailabilityThreshold = 80m,
    int DataRetentionDays = 90
);
