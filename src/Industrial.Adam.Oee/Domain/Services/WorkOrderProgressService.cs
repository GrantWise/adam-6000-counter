using Industrial.Adam.Oee.Domain.Entities;
using Industrial.Adam.Oee.Domain.Enums;
using Industrial.Adam.Oee.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Domain.Services;

/// <summary>
/// Domain service for work order progress calculations and analysis
/// Extracts complex progress logic from WorkOrder entity to follow SRP
/// </summary>
public interface IWorkOrderProgressService
{
    /// <summary>
    /// Calculate completion percentage based on planned quantity
    /// </summary>
    /// <param name="workOrder">Work order</param>
    /// <returns>Completion percentage (0-100)</returns>
    public decimal CalculateCompletionPercentage(WorkOrder workOrder);

    /// <summary>
    /// Calculate yield/quality percentage
    /// </summary>
    /// <param name="workOrder">Work order</param>
    /// <returns>Yield percentage (0-100)</returns>
    public decimal CalculateYieldPercentage(WorkOrder workOrder);

    /// <summary>
    /// Check if work order is behind schedule
    /// </summary>
    /// <param name="workOrder">Work order</param>
    /// <returns>True if behind schedule</returns>
    public bool IsBehindSchedule(WorkOrder workOrder);

    /// <summary>
    /// Get production rate (pieces per minute)
    /// </summary>
    /// <param name="workOrder">Work order</param>
    /// <returns>Production rate in pieces per minute</returns>
    public decimal GetProductionRate(WorkOrder workOrder);

    /// <summary>
    /// Calculate estimated completion time based on current rate
    /// </summary>
    /// <param name="workOrder">Work order</param>
    /// <returns>Estimated completion time or null if cannot be calculated</returns>
    public DateTime? GetEstimatedCompletionTime(WorkOrder workOrder);

    /// <summary>
    /// Check if work order requires attention
    /// </summary>
    /// <param name="workOrder">Work order</param>
    /// <param name="qualityThreshold">Quality threshold percentage (default 95%)</param>
    /// <returns>True if requires attention</returns>
    public bool RequiresAttention(WorkOrder workOrder, decimal qualityThreshold = 95m);

    /// <summary>
    /// Analyze work order performance
    /// </summary>
    /// <param name="workOrder">Work order</param>
    /// <returns>Performance analysis</returns>
    public WorkOrderPerformanceAnalysis AnalyzePerformance(WorkOrder workOrder);

    /// <summary>
    /// Calculate expected progress based on schedule
    /// </summary>
    /// <param name="workOrder">Work order</param>
    /// <param name="currentTime">Current time (optional, uses UtcNow if not provided)</param>
    /// <returns>Expected progress percentage</returns>
    public decimal CalculateExpectedProgress(WorkOrder workOrder, DateTime? currentTime = null);
}

/// <summary>
/// Implementation of work order progress service
/// </summary>
public sealed class WorkOrderProgressService : IWorkOrderProgressService
{
    private readonly ILogger<WorkOrderProgressService> _logger;

    /// <summary>
    /// Initialize work order progress service
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public WorkOrderProgressService(ILogger<WorkOrderProgressService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public decimal CalculateCompletionPercentage(WorkOrder workOrder)
    {
        if (workOrder == null)
            throw new ArgumentNullException(nameof(workOrder));

        if (workOrder.PlannedQuantity == 0)
        {
            _logger.LogWarning("Work order {WorkOrderId} has zero planned quantity", workOrder.Id);
            return 0;
        }

        var totalProduced = workOrder.TotalQuantityProduced;
        var completionPercentage = (totalProduced / workOrder.PlannedQuantity) * 100;

        // Cap at 100% for display purposes
        var cappedPercentage = Math.Min(completionPercentage, 100);

        _logger.LogDebug(
            "Work order {WorkOrderId} completion: {Completion:F1}% ({Produced}/{Planned})",
            workOrder.Id, cappedPercentage, totalProduced, workOrder.PlannedQuantity);

        return cappedPercentage;
    }

    /// <inheritdoc />
    public decimal CalculateYieldPercentage(WorkOrder workOrder)
    {
        if (workOrder == null)
            throw new ArgumentNullException(nameof(workOrder));

        var total = workOrder.TotalQuantityProduced;
        if (total == 0)
        {
            _logger.LogDebug("Work order {WorkOrderId} has no production, returning 100% yield", workOrder.Id);
            return 100; // No production = no defects
        }

        var yieldPercentage = (workOrder.ActualQuantityGood / total) * 100;

        _logger.LogDebug(
            "Work order {WorkOrderId} yield: {Yield:F1}% ({Good}/{Total})",
            workOrder.Id, yieldPercentage, workOrder.ActualQuantityGood, total);

        return yieldPercentage;
    }

    /// <inheritdoc />
    public bool IsBehindSchedule(WorkOrder workOrder)
    {
        if (workOrder == null)
            throw new ArgumentNullException(nameof(workOrder));

        var now = DateTime.UtcNow;
        var expectedProgress = CalculateExpectedProgress(workOrder, now);
        var actualProgress = CalculateCompletionPercentage(workOrder);

        var isBehind = actualProgress < expectedProgress;

        _logger.LogDebug(
            "Work order {WorkOrderId} schedule check: Actual={Actual:F1}%, Expected={Expected:F1}%, Behind={Behind}",
            workOrder.Id, actualProgress, expectedProgress, isBehind);

        return isBehind;
    }

    /// <inheritdoc />
    public decimal GetProductionRate(WorkOrder workOrder)
    {
        if (workOrder == null)
            throw new ArgumentNullException(nameof(workOrder));

        if (workOrder.ActualStartTime == null || workOrder.Status == WorkOrderStatus.Pending)
        {
            _logger.LogDebug("Work order {WorkOrderId} not started, returning zero rate", workOrder.Id);
            return 0;
        }

        var endTime = workOrder.ActualEndTime ?? DateTime.UtcNow;
        var durationMinutes = (decimal)(endTime - workOrder.ActualStartTime.Value).TotalMinutes;

        if (durationMinutes == 0)
        {
            _logger.LogDebug("Work order {WorkOrderId} has zero duration, returning zero rate", workOrder.Id);
            return 0;
        }

        var rate = workOrder.TotalQuantityProduced / durationMinutes;

        _logger.LogDebug(
            "Work order {WorkOrderId} production rate: {Rate:F2} pieces/minute over {Duration:F1} minutes",
            workOrder.Id, rate, durationMinutes);

        return rate;
    }

    /// <inheritdoc />
    public DateTime? GetEstimatedCompletionTime(WorkOrder workOrder)
    {
        if (workOrder == null)
            throw new ArgumentNullException(nameof(workOrder));

        var rate = GetProductionRate(workOrder);
        if (rate == 0)
        {
            _logger.LogDebug("Work order {WorkOrderId} has zero production rate, cannot estimate completion", workOrder.Id);
            return null;
        }

        var remainingQuantity = workOrder.PlannedQuantity - workOrder.TotalQuantityProduced;
        if (remainingQuantity <= 0)
        {
            _logger.LogDebug("Work order {WorkOrderId} is complete or overproduced", workOrder.Id);
            return DateTime.UtcNow;
        }

        var remainingMinutes = remainingQuantity / rate;
        var estimatedCompletion = DateTime.UtcNow.AddMinutes((double)remainingMinutes);

        _logger.LogDebug(
            "Work order {WorkOrderId} estimated completion: {EstimatedTime} ({RemainingMinutes:F1} minutes remaining)",
            workOrder.Id, estimatedCompletion, remainingMinutes);

        return estimatedCompletion;
    }

    /// <inheritdoc />
    public bool RequiresAttention(WorkOrder workOrder, decimal qualityThreshold = 95m)
    {
        if (workOrder == null)
            throw new ArgumentNullException(nameof(workOrder));

        var reasons = new List<string>();

        if (IsBehindSchedule(workOrder))
        {
            reasons.Add("behind schedule");
        }

        if (CalculateYieldPercentage(workOrder) < qualityThreshold)
        {
            reasons.Add($"yield below {qualityThreshold}%");
        }

        if (workOrder.Status == WorkOrderStatus.Active && GetProductionRate(workOrder) == 0)
        {
            reasons.Add("no production activity");
        }

        var requiresAttention = reasons.Count > 0;

        if (requiresAttention)
        {
            _logger.LogWarning(
                "Work order {WorkOrderId} requires attention: {Reasons}",
                workOrder.Id, string.Join(", ", reasons));
        }
        else
        {
            _logger.LogDebug("Work order {WorkOrderId} is performing normally", workOrder.Id);
        }

        return requiresAttention;
    }

    /// <inheritdoc />
    public WorkOrderPerformanceAnalysis AnalyzePerformance(WorkOrder workOrder)
    {
        if (workOrder == null)
            throw new ArgumentNullException(nameof(workOrder));

        var completionPercentage = CalculateCompletionPercentage(workOrder);
        var yieldPercentage = CalculateYieldPercentage(workOrder);
        var productionRate = GetProductionRate(workOrder);
        var estimatedCompletion = GetEstimatedCompletionTime(workOrder);
        var isBehindSchedule = IsBehindSchedule(workOrder);
        var requiresAttention = RequiresAttention(workOrder);
        var expectedProgress = CalculateExpectedProgress(workOrder);

        var performanceRating = CalculatePerformanceRating(completionPercentage, yieldPercentage, isBehindSchedule);
        var efficiency = expectedProgress > 0 ? completionPercentage / expectedProgress : 1.0m;

        var analysis = new WorkOrderPerformanceAnalysis(
            CompletionPercentage: completionPercentage,
            YieldPercentage: yieldPercentage,
            ProductionRate: productionRate,
            ExpectedProgress: expectedProgress,
            IsBehindSchedule: isBehindSchedule,
            RequiresAttention: requiresAttention,
            EstimatedCompletion: estimatedCompletion,
            PerformanceRating: performanceRating,
            EfficiencyRatio: efficiency);

        _logger.LogInformation(
            "Work order {WorkOrderId} performance analysis: Rating={Rating}, Completion={Completion:F1}%, Yield={Yield:F1}%, Rate={Rate:F2}/min",
            workOrder.Id, performanceRating, completionPercentage, yieldPercentage, productionRate);

        return analysis;
    }

    /// <inheritdoc />
    public decimal CalculateExpectedProgress(WorkOrder workOrder, DateTime? currentTime = null)
    {
        if (workOrder == null)
            throw new ArgumentNullException(nameof(workOrder));

        var now = currentTime ?? DateTime.UtcNow;

        // If not started yet, expected progress is 0
        if (now < workOrder.ScheduledStartTime)
        {
            return 0;
        }

        // If past scheduled end time, expected progress is 100%
        if (now >= workOrder.ScheduledEndTime)
        {
            return 100;
        }

        var scheduledDuration = (workOrder.ScheduledEndTime - workOrder.ScheduledStartTime).TotalMilliseconds;
        var elapsedTime = (now - workOrder.ScheduledStartTime).TotalMilliseconds;
        var expectedProgress = (decimal)(elapsedTime / scheduledDuration) * 100;

        _logger.LogDebug(
            "Work order {WorkOrderId} expected progress: {Progress:F1}% ({Elapsed:F1}/{Total:F1} hours)",
            workOrder.Id, expectedProgress, elapsedTime / 3600000, scheduledDuration / 3600000);

        return Math.Max(0, Math.Min(100, expectedProgress));
    }

    /// <summary>
    /// Calculate performance rating based on key metrics
    /// </summary>
    /// <param name="completion">Completion percentage</param>
    /// <param name="yield">Yield percentage</param>
    /// <param name="isBehindSchedule">Whether behind schedule</param>
    /// <returns>Performance rating</returns>
    private static PerformanceRating CalculatePerformanceRating(decimal completion, decimal yield, bool isBehindSchedule)
    {
        // Start with excellent and downgrade based on issues
        var rating = PerformanceRating.Excellent;

        if (yield < 95)
        {
            rating = PerformanceRating.Good;
        }

        if (yield < 90 || isBehindSchedule)
        {
            rating = PerformanceRating.Fair;
        }

        if (yield < 80 || (isBehindSchedule && completion < 50))
        {
            rating = PerformanceRating.Poor;
        }

        if (yield < 70 || (isBehindSchedule && completion < 25))
        {
            rating = PerformanceRating.Critical;
        }

        return rating;
    }
}

/// <summary>
/// Comprehensive work order performance analysis
/// </summary>
public sealed record WorkOrderPerformanceAnalysis(
    decimal CompletionPercentage,
    decimal YieldPercentage,
    decimal ProductionRate,
    decimal ExpectedProgress,
    bool IsBehindSchedule,
    bool RequiresAttention,
    DateTime? EstimatedCompletion,
    PerformanceRating PerformanceRating,
    decimal EfficiencyRatio)
{
    /// <summary>
    /// Overall performance score (0-100)
    /// </summary>
    public decimal OverallScore => (CompletionPercentage * 0.4m + YieldPercentage * 0.4m + (IsBehindSchedule ? 0 : 20)) * EfficiencyRatio;

    /// <summary>
    /// Performance summary text
    /// </summary>
    public string Summary => $"{PerformanceRating} - {CompletionPercentage:F1}% complete, {YieldPercentage:F1}% yield, {ProductionRate:F1}/min";
}

/// <summary>
/// Performance rating enumeration
/// </summary>
public enum PerformanceRating
{
    /// <summary>
    /// Critical performance issues requiring immediate attention
    /// </summary>
    Critical = 1,

    /// <summary>
    /// Poor performance with significant issues
    /// </summary>
    Poor = 2,

    /// <summary>
    /// Fair performance with some concerns
    /// </summary>
    Fair = 3,

    /// <summary>
    /// Good performance meeting most targets
    /// </summary>
    Good = 4,

    /// <summary>
    /// Excellent performance exceeding expectations
    /// </summary>
    Excellent = 5
}
