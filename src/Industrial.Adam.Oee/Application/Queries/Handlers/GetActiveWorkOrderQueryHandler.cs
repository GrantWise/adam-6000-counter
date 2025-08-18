using Industrial.Adam.Oee.Application.DTOs;
using Industrial.Adam.Oee.Application.Queries;
using Industrial.Adam.Oee.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Application.Queries.Handlers;

/// <summary>
/// Handler for GetActiveWorkOrderQuery
/// </summary>
public class GetActiveWorkOrderQueryHandler : IRequestHandler<GetActiveWorkOrderQuery, WorkOrderDto?>
{
    private readonly IWorkOrderRepository _workOrderRepository;
    private readonly ILogger<GetActiveWorkOrderQueryHandler> _logger;

    public GetActiveWorkOrderQueryHandler(
        IWorkOrderRepository workOrderRepository,
        ILogger<GetActiveWorkOrderQueryHandler> logger)
    {
        _workOrderRepository = workOrderRepository;
        _logger = logger;
    }

    public async Task<WorkOrderDto?> Handle(GetActiveWorkOrderQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting active work order for device {DeviceId}", request.DeviceId);

        try
        {
            var activeWorkOrder = await _workOrderRepository.GetActiveByDeviceAsync(request.DeviceId, cancellationToken);

            if (activeWorkOrder == null)
            {
                _logger.LogInformation("No active work order found for device {DeviceId}", request.DeviceId);
                return null;
            }

            var result = WorkOrderDto.FromDomain(activeWorkOrder);

            _logger.LogInformation("Found active work order {WorkOrderId} for device {DeviceId}",
                result.WorkOrderId, request.DeviceId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active work order for device {DeviceId}", request.DeviceId);
            throw;
        }
    }
}
