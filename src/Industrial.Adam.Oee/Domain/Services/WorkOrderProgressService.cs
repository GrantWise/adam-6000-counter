using Industrial.Adam.Oee.Domain.Entities;
using Industrial.Adam.Oee.Domain.Enums;
using Industrial.Adam.Oee.Domain.Exceptions;
using Industrial.Adam.Oee.Domain.Interfaces;
using Industrial.Adam.Oee.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Domain.Services;

/// <summary>
/// Implementation of work order progress service
/// </summary>
public sealed class WorkOrderProgressService : IWorkOrderProgressService
{
    private readonly IWorkOrderRepository _workOrderRepository;
    private readonly ICounterDataRepository _counterDataRepository;
    private readonly ILogger<WorkOrderProgressService> _logger;

    /// <summary>
    /// Initialize work order progress service
    /// </summary>
    /// <param name="workOrderRepository">Work order repository</param>
    /// <param name="counterDataRepository">Counter data repository</param>
    /// <param name="logger">Logger instance</param>
    public WorkOrderProgressService(
        IWorkOrderRepository workOrderRepository,
        ICounterDataRepository counterDataRepository,
        ILogger<WorkOrderProgressService> logger)
    {
        _workOrderRepository = workOrderRepository ?? throw new ArgumentNullException(nameof(workOrderRepository));
        _counterDataRepository = counterDataRepository ?? throw new ArgumentNullException(nameof(counterDataRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<WorkOrderProgress> GetProgressAsync(
        string workOrderId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workOrderId))
            throw new ArgumentException("Work order ID cannot be null or empty", nameof(workOrderId));

        _logger.LogDebug("Getting progress for work order {WorkOrderId}", workOrderId);

        try
        {
            var workOrder = await _workOrderRepository.GetByIdAsync(workOrderId, cancellationToken);
            if (workOrder == null)
            {
                throw new OeeCalculationException(
                    $"Work order {workOrderId} not found",
                    "WorkOrderProgress",
                    OeeErrorCode.WorkOrderNotFound,
                    null,
                    null,
                    null);
            }

            var completionPercentage = await CalculateCompletionPercentageAsync(workOrder, cancellationToken);
            var remainingQuantity = Math.Max(0, workOrder.PlannedQuantity - workOrder.TotalQuantityProduced);
            var estimatedCompletion = await PredictCompletionTimeAsync(workOrderId, cancellationToken);
            var isOnSchedule = workOrder.ScheduledEndTime > DateTime.UtcNow ||
                              (estimatedCompletion.HasValue && estimatedCompletion <= workOrder.ScheduledEndTime);

            return new WorkOrderProgress(
                workOrderId,
                completionPercentage,
                workOrder.ActualQuantityGood,
                workOrder.ActualQuantityScrap,
                workOrder.PlannedQuantity,
                remainingQuantity,
                estimatedCompletion,
                isOnSchedule,
                DateTime.UtcNow
            );
        }
        catch (Exception ex) when (!(ex is OeeCalculationException))
        {
            var calculationException = new OeeCalculationException(
                $"Failed to get progress for work order {workOrderId}",
                "WorkOrderProgress",
                OeeErrorCode.CalculationFailed,
                null,
                null,
                null,
                ex);

            _logger.LogError(calculationException,
                "Work order progress calculation failed for {WorkOrderId}", workOrderId);

            throw calculationException;
        }
    }

    /// <inheritdoc />
    public async Task<WorkOrderProgress> UpdateProgressAsync(
        string workOrderId,
        decimal goodCount,
        decimal scrapCount,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workOrderId))
            throw new ArgumentException("Work order ID cannot be null or empty", nameof(workOrderId));

        _logger.LogDebug(
            "Updating progress for work order {WorkOrderId}: {GoodCount} good, {ScrapCount} scrap",
            workOrderId, goodCount, scrapCount);

        try
        {
            var workOrder = await _workOrderRepository.GetByIdAsync(workOrderId, cancellationToken);
            if (workOrder == null)
            {
                throw new OeeCalculationException(
                    $"Work order {workOrderId} not found",
                    "WorkOrderProgressUpdate",
                    OeeErrorCode.WorkOrderNotFound,
                    null,
                    null,
                    null);
            }

            // Update work order quantities
            workOrder.UpdateFromCounterData(goodCount, scrapCount);
            await _workOrderRepository.UpdateAsync(workOrder, cancellationToken);

            return await GetProgressAsync(workOrderId, cancellationToken);
        }
        catch (Exception ex) when (!(ex is OeeCalculationException))
        {
            var calculationException = new OeeCalculationException(
                $"Failed to update progress for work order {workOrderId}",
                "WorkOrderProgressUpdate",
                OeeErrorCode.CalculationFailed,
                null,
                null,
                null,
                ex);

            _logger.LogError(calculationException,
                "Work order progress update failed for {WorkOrderId}", workOrderId);

            throw calculationException;
        }
    }

    /// <inheritdoc />
    public async Task<decimal> CalculateCompletionPercentageAsync(
        WorkOrder workOrder,
        CancellationToken cancellationToken = default)
    {
        if (workOrder == null)
            throw new ArgumentNullException(nameof(workOrder));

        await Task.CompletedTask; // Method is synchronous but interface is async

        if (workOrder.PlannedQuantity <= 0)
            return 0;

        var completionPercentage = (workOrder.TotalQuantityProduced / workOrder.PlannedQuantity) * 100;
        return Math.Min(100, completionPercentage);
    }

    /// <inheritdoc />
    public async Task<WorkOrderEfficiency> GetEfficiencyMetricsAsync(
        string workOrderId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workOrderId))
            throw new ArgumentException("Work order ID cannot be null or empty", nameof(workOrderId));

        _logger.LogDebug("Getting efficiency metrics for work order {WorkOrderId}", workOrderId);

        try
        {
            var workOrder = await _workOrderRepository.GetByIdAsync(workOrderId, cancellationToken);
            if (workOrder == null)
            {
                throw new OeeCalculationException(
                    $"Work order {workOrderId} not found",
                    "WorkOrderEfficiency",
                    OeeErrorCode.WorkOrderNotFound,
                    null,
                    null,
                    null);
            }

            var yieldRate = workOrder.TotalQuantityProduced > 0
                ? (workOrder.ActualQuantityGood / workOrder.TotalQuantityProduced) * 100
                : 0;

            var qualityRate = yieldRate; // Same as yield rate for this implementation

            var elapsedTime = (workOrder.ActualEndTime ?? DateTime.UtcNow) -
                             (workOrder.ActualStartTime ?? workOrder.ScheduledStartTime);

            var throughputRate = elapsedTime.TotalMinutes > 0
                ? workOrder.TotalQuantityProduced / (decimal)elapsedTime.TotalMinutes
                : 0;

            var scheduleTime = workOrder.ScheduledEndTime - workOrder.ScheduledStartTime;
            var scheduleAdherence = scheduleTime.TotalMinutes > 0 && elapsedTime.TotalMinutes > 0
                ? Math.Min(100, (scheduleTime.TotalMinutes / elapsedTime.TotalMinutes) * 100)
                : 100;

            var overallEfficiency = (yieldRate * (decimal)scheduleAdherence) / 100;

            return new WorkOrderEfficiency(
                workOrderId,
                overallEfficiency,
                yieldRate,
                throughputRate,
                (decimal)scheduleAdherence,
                qualityRate
            );
        }
        catch (Exception ex) when (!(ex is OeeCalculationException))
        {
            var calculationException = new OeeCalculationException(
                $"Failed to get efficiency metrics for work order {workOrderId}",
                "WorkOrderEfficiency",
                OeeErrorCode.CalculationFailed,
                null,
                null,
                null,
                ex);

            _logger.LogError(calculationException,
                "Work order efficiency calculation failed for {WorkOrderId}", workOrderId);

            throw calculationException;
        }
    }

    /// <inheritdoc />
    public async Task<DateTime?> PredictCompletionTimeAsync(
        string workOrderId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workOrderId))
            throw new ArgumentException("Work order ID cannot be null or empty", nameof(workOrderId));

        try
        {
            var workOrder = await _workOrderRepository.GetByIdAsync(workOrderId, cancellationToken);
            if (workOrder == null)
                return null;

            // If already completed
            if (workOrder.ActualEndTime.HasValue)
                return workOrder.ActualEndTime;

            // If not started
            if (!workOrder.ActualStartTime.HasValue)
                return null;

            var elapsedTime = DateTime.UtcNow - workOrder.ActualStartTime.Value;

            // Need some production to calculate rate
            if (workOrder.TotalQuantityProduced <= 0 || elapsedTime.TotalMinutes <= 0)
                return null;

            var currentRate = workOrder.TotalQuantityProduced / (decimal)elapsedTime.TotalMinutes;
            var remainingQuantity = Math.Max(0, workOrder.PlannedQuantity - workOrder.TotalQuantityProduced);

            if (currentRate <= 0)
                return null;

            var remainingMinutes = remainingQuantity / currentRate;
            return DateTime.UtcNow.AddMinutes((double)remainingMinutes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to predict completion time for work order {WorkOrderId}", workOrderId);
            return null;
        }
    }
}
