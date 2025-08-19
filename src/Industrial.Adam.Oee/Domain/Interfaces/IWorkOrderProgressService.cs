using Industrial.Adam.Oee.Domain.Entities;
using Industrial.Adam.Oee.Domain.ValueObjects;

namespace Industrial.Adam.Oee.Domain.Interfaces;

/// <summary>
/// Domain service for work order progress tracking
/// </summary>
public interface IWorkOrderProgressService
{
    /// <summary>
    /// Get current progress for a work order
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Work order progress information</returns>
    public Task<WorkOrderProgress> GetProgressAsync(
        string workOrderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update work order progress with production counts
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="goodCount">Number of good pieces produced</param>
    /// <param name="scrapCount">Number of scrapped pieces</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated work order progress</returns>
    public Task<WorkOrderProgress> UpdateProgressAsync(
        string workOrderId,
        decimal goodCount,
        decimal scrapCount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate work order completion percentage
    /// </summary>
    /// <param name="workOrder">Work order</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Completion percentage</returns>
    public Task<decimal> CalculateCompletionPercentageAsync(
        WorkOrder workOrder,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get work order efficiency metrics
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Work order efficiency metrics</returns>
    public Task<WorkOrderEfficiency> GetEfficiencyMetricsAsync(
        string workOrderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Predict work order completion time based on current progress
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Predicted completion time</returns>
    public Task<DateTime?> PredictCompletionTimeAsync(
        string workOrderId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Work order progress information
/// </summary>
/// <param name="WorkOrderId">Work order identifier</param>
/// <param name="CompletionPercentage">Completion percentage</param>
/// <param name="ActualGoodCount">Actual good pieces produced</param>
/// <param name="ActualScrapCount">Actual scrapped pieces</param>
/// <param name="PlannedQuantity">Planned quantity</param>
/// <param name="RemainingQuantity">Remaining quantity to produce</param>
/// <param name="EstimatedCompletionTime">Estimated completion time</param>
/// <param name="IsOnSchedule">Whether work order is on schedule</param>
/// <param name="LastUpdated">When progress was last updated</param>
public record WorkOrderProgress(
    string WorkOrderId,
    decimal CompletionPercentage,
    decimal ActualGoodCount,
    decimal ActualScrapCount,
    decimal PlannedQuantity,
    decimal RemainingQuantity,
    DateTime? EstimatedCompletionTime,
    bool IsOnSchedule,
    DateTime LastUpdated
);

/// <summary>
/// Work order efficiency metrics
/// </summary>
/// <param name="WorkOrderId">Work order identifier</param>
/// <param name="OverallEfficiency">Overall efficiency percentage</param>
/// <param name="YieldRate">Yield rate percentage</param>
/// <param name="ThroughputRate">Throughput rate (units per minute)</param>
/// <param name="ScheduleAdherence">Schedule adherence percentage</param>
/// <param name="QualityRate">Quality rate percentage</param>
public record WorkOrderEfficiency(
    string WorkOrderId,
    decimal OverallEfficiency,
    decimal YieldRate,
    decimal ThroughputRate,
    decimal ScheduleAdherence,
    decimal QualityRate
);
