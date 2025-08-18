using Industrial.Adam.Oee.Application.DTOs;
using Industrial.Adam.Oee.Application.Queries;
using Industrial.Adam.Oee.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Application.Queries.Handlers;

/// <summary>
/// Handler for GetWorkOrderQuery
/// </summary>
public class GetWorkOrderQueryHandler : IRequestHandler<GetWorkOrderQuery, WorkOrderDto?>
{
    private readonly IWorkOrderRepository _workOrderRepository;
    private readonly ILogger<GetWorkOrderQueryHandler> _logger;

    /// <summary>
    /// Constructor for GetWorkOrderQueryHandler
    /// </summary>
    /// <param name="workOrderRepository">Work order repository</param>
    /// <param name="logger">Logger instance</param>
    public GetWorkOrderQueryHandler(
        IWorkOrderRepository workOrderRepository,
        ILogger<GetWorkOrderQueryHandler> logger)
    {
        _workOrderRepository = workOrderRepository;
        _logger = logger;
    }

    /// <summary>
    /// Handle the GetWorkOrderQuery
    /// </summary>
    /// <param name="request">Query request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Work order DTO or null if not found</returns>
    public async Task<WorkOrderDto?> Handle(GetWorkOrderQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting work order with ID: {WorkOrderId}", request.WorkOrderId);

        if (string.IsNullOrWhiteSpace(request.WorkOrderId))
        {
            throw new ArgumentException("Work order ID cannot be null or empty", nameof(request.WorkOrderId));
        }

        var workOrder = await _workOrderRepository.GetByIdAsync(request.WorkOrderId);

        if (workOrder == null)
        {
            _logger.LogInformation("Work order with ID {WorkOrderId} not found", request.WorkOrderId);
            return null;
        }

        _logger.LogInformation("Successfully retrieved work order {WorkOrderId} with status {Status}",
            workOrder.Id, workOrder.Status);

        return WorkOrderDto.FromDomain(workOrder);
    }
}
