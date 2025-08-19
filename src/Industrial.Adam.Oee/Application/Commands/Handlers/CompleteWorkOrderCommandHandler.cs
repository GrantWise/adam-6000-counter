using Industrial.Adam.Oee.Application.Commands;
using Industrial.Adam.Oee.Domain.Entities;
using Industrial.Adam.Oee.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Application.Commands.Handlers;

/// <summary>
/// Handler for CompleteWorkOrderCommand
/// </summary>
public class CompleteWorkOrderCommandHandler : IRequestHandler<CompleteWorkOrderCommand>
{
    private readonly IWorkOrderRepository _workOrderRepository;
    private readonly ICounterDataRepository _counterDataRepository;
    private readonly ILogger<CompleteWorkOrderCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the CompleteWorkOrderCommandHandler class
    /// </summary>
    /// <param name="workOrderRepository">Repository for work order operations</param>
    /// <param name="counterDataRepository">Repository for counter data operations</param>
    /// <param name="logger">Logger instance</param>
    public CompleteWorkOrderCommandHandler(
        IWorkOrderRepository workOrderRepository,
        ICounterDataRepository counterDataRepository,
        ILogger<CompleteWorkOrderCommandHandler> logger)
    {
        _workOrderRepository = workOrderRepository;
        _counterDataRepository = counterDataRepository;
        _logger = logger;
    }

    /// <summary>
    /// Handles the CompleteWorkOrderCommand request
    /// </summary>
    /// <param name="request">The command request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task Handle(CompleteWorkOrderCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Completing work order {WorkOrderId}", request.WorkOrderId);

        try
        {
            // Get the work order
            var workOrder = await _workOrderRepository.GetByIdAsync(request.WorkOrderId, cancellationToken);
            if (workOrder == null)
            {
                throw new InvalidOperationException($"Work order {request.WorkOrderId} not found");
            }

            // Validate that work order can be completed
            if (!workOrder.IsActive && workOrder.Status != WorkOrderStatus.Paused)
            {
                throw new InvalidOperationException(
                    $"Cannot complete work order {request.WorkOrderId} with status {workOrder.Status}");
            }

            // Update final quantities if provided, otherwise use current counter data
            if (request.FinalGoodQuantity.HasValue || request.FinalScrapQuantity.HasValue)
            {
                var goodQuantity = request.FinalGoodQuantity ?? workOrder.ActualQuantityGood;
                var scrapQuantity = request.FinalScrapQuantity ?? workOrder.ActualQuantityScrap;

                _logger.LogDebug("Updating work order {WorkOrderId} with final quantities: Good={GoodQuantity}, Scrap={ScrapQuantity}",
                    request.WorkOrderId, goodQuantity, scrapQuantity);

                workOrder.UpdateFromCounterData(goodQuantity, scrapQuantity);
            }
            else
            {
                // Update with latest counter data
                await UpdateWorkOrderWithLatestCounterDataAsync(workOrder, cancellationToken);
            }

            // Complete the work order
            workOrder.Complete();

            // Save the updated work order
            var updated = await _workOrderRepository.UpdateAsync(workOrder, cancellationToken);
            if (!updated)
            {
                throw new InvalidOperationException($"Failed to update work order {request.WorkOrderId}");
            }

            _logger.LogInformation("Successfully completed work order {WorkOrderId}. Final quantities: Good={GoodQuantity}, Scrap={ScrapQuantity}, Yield={YieldPercentage:F1}%",
                request.WorkOrderId,
                workOrder.ActualQuantityGood,
                workOrder.ActualQuantityScrap,
                workOrder.GetYieldPercentage());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing work order {WorkOrderId}", request.WorkOrderId);
            throw;
        }
    }

    /// <summary>
    /// Update work order with latest counter data
    /// </summary>
    /// <param name="workOrder">Work order to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task UpdateWorkOrderWithLatestCounterDataAsync(
        Domain.Entities.WorkOrder workOrder,
        CancellationToken cancellationToken)
    {
        try
        {
            var latestChannel0 = await _counterDataRepository.GetLatestReadingAsync(
                workOrder.ResourceReference, 0, cancellationToken);
            var latestChannel1 = await _counterDataRepository.GetLatestReadingAsync(
                workOrder.ResourceReference, 1, cancellationToken);

            if (latestChannel0 != null || latestChannel1 != null)
            {
                var goodCount = latestChannel0?.ProcessedValue ?? 0;
                var scrapCount = latestChannel1?.ProcessedValue ?? 0;

                _logger.LogDebug("Updating work order {WorkOrderId} with latest counter data: Good={GoodCount}, Scrap={ScrapCount}",
                    workOrder.Id, goodCount, scrapCount);

                workOrder.UpdateFromCounterData(goodCount, scrapCount);
            }
            else
            {
                _logger.LogWarning("No counter data found for device {DeviceId} when completing work order {WorkOrderId}",
                    workOrder.ResourceReference, workOrder.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting latest counter data for device {DeviceId} when completing work order {WorkOrderId}",
                workOrder.ResourceReference, workOrder.Id);
            // Continue with completion using existing quantities
        }
    }
}
