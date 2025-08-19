using Industrial.Adam.Oee.Application.DTOs;
using Industrial.Adam.Oee.Application.Queries;
using Industrial.Adam.Oee.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Application.Queries.Handlers;

/// <summary>
/// Handler for GetWorkOrderProgressQuery
/// </summary>
public class GetWorkOrderProgressQueryHandler : IRequestHandler<GetWorkOrderProgressQuery, WorkOrderProgressDto?>
{
    private readonly IWorkOrderRepository _workOrderRepository;
    private readonly ILogger<GetWorkOrderProgressQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the GetWorkOrderProgressQueryHandler class
    /// </summary>
    /// <param name="workOrderRepository">Repository for work order operations</param>
    /// <param name="logger">Logger instance</param>
    public GetWorkOrderProgressQueryHandler(
        IWorkOrderRepository workOrderRepository,
        ILogger<GetWorkOrderProgressQueryHandler> logger)
    {
        _workOrderRepository = workOrderRepository;
        _logger = logger;
    }

    /// <summary>
    /// Handles the GetWorkOrderProgressQuery request
    /// </summary>
    /// <param name="request">The query request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation with work order progress data</returns>
    public async Task<WorkOrderProgressDto?> Handle(GetWorkOrderProgressQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting work order progress for {WorkOrderId}", request.WorkOrderId);

        try
        {
            var workOrder = await _workOrderRepository.GetByIdAsync(request.WorkOrderId, cancellationToken);

            if (workOrder == null)
            {
                _logger.LogInformation("Work order {WorkOrderId} not found", request.WorkOrderId);
                return null;
            }

            var result = new WorkOrderProgressDto
            {
                WorkOrderId = workOrder.Id,
                ProductDescription = workOrder.ProductDescription,
                Status = workOrder.Status.ToString(),
                CompletionPercentage = workOrder.GetCompletionPercentage(),
                YieldPercentage = workOrder.GetYieldPercentage(),
                ProductionRate = workOrder.GetProductionRate(),
                PlannedQuantity = workOrder.PlannedQuantity,
                ActualQuantityGood = workOrder.ActualQuantityGood,
                ActualQuantityScrap = workOrder.ActualQuantityScrap,
                TotalQuantityProduced = workOrder.TotalQuantityProduced,
                IsBehindSchedule = workOrder.IsBehindSchedule(),
                RequiresAttention = workOrder.RequiresAttention(),
                EstimatedCompletionTime = workOrder.GetEstimatedCompletionTime(),
                ActualStartTime = workOrder.ActualStartTime,
                ScheduledEndTime = workOrder.ScheduledEndTime,
                LastUpdate = workOrder.UpdatedAt
            };

            _logger.LogInformation("Work order {WorkOrderId} progress: {CompletionPercentage:F1}% complete, {YieldPercentage:F1}% yield",
                request.WorkOrderId, result.CompletionPercentage, result.YieldPercentage);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting work order progress for {WorkOrderId}", request.WorkOrderId);
            throw;
        }
    }
}
