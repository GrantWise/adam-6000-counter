using Industrial.Adam.Oee.Application.DTOs;
using Industrial.Adam.Oee.Application.Queries;
using Industrial.Adam.Oee.Domain.Entities;
using Industrial.Adam.Oee.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Application.Queries.Handlers;

/// <summary>
/// Handler for CalculateCurrentOeeQuery
/// </summary>
public class CalculateCurrentOeeQueryHandler : IRequestHandler<CalculateCurrentOeeQuery, OeeCalculationDto>
{
    private readonly IOeeCalculationService _oeeCalculationService;
    private readonly IWorkOrderRepository _workOrderRepository;
    private readonly ILogger<CalculateCurrentOeeQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the CalculateCurrentOeeQueryHandler class
    /// </summary>
    /// <param name="oeeCalculationService">Service for OEE calculations</param>
    /// <param name="workOrderRepository">Repository for work order operations</param>
    /// <param name="logger">Logger instance</param>
    public CalculateCurrentOeeQueryHandler(
        IOeeCalculationService oeeCalculationService,
        IWorkOrderRepository workOrderRepository,
        ILogger<CalculateCurrentOeeQueryHandler> logger)
    {
        _oeeCalculationService = oeeCalculationService;
        _workOrderRepository = workOrderRepository;
        _logger = logger;
    }

    /// <summary>
    /// Handles the CalculateCurrentOeeQuery request
    /// </summary>
    /// <param name="request">The query request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation with OEE calculation results</returns>
    public async Task<OeeCalculationDto> Handle(CalculateCurrentOeeQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Calculating current OEE for device {DeviceId}", request.DeviceId);

        try
        {
            OeeCalculation oeeCalculation;

            // If specific time range is provided, calculate for that period
            if (request.StartTime.HasValue && request.EndTime.HasValue)
            {
                _logger.LogDebug("Calculating OEE for period {StartTime} to {EndTime}",
                    request.StartTime.Value, request.EndTime.Value);

                oeeCalculation = await _oeeCalculationService.CalculateOeeForPeriodAsync(
                    request.DeviceId,
                    request.StartTime.Value,
                    request.EndTime.Value,
                    cancellationToken);
            }
            else
            {
                // Get active work order to determine calculation period
                var activeWorkOrder = await _workOrderRepository.GetActiveByDeviceAsync(request.DeviceId, cancellationToken);

                if (activeWorkOrder != null)
                {
                    _logger.LogDebug("Found active work order {WorkOrderId}, calculating OEE for work order context",
                        activeWorkOrder.Id);

                    oeeCalculation = await _oeeCalculationService.CalculateOeeForWorkOrderAsync(
                        activeWorkOrder,
                        cancellationToken);
                }
                else
                {
                    _logger.LogDebug("No active work order found, calculating current OEE");

                    oeeCalculation = await _oeeCalculationService.CalculateCurrentOeeAsync(
                        request.DeviceId,
                        cancellationToken);
                }
            }

            var result = OeeCalculationDto.FromDomain(oeeCalculation);

            _logger.LogInformation("Successfully calculated OEE: {OeePercentage:F1}% for device {DeviceId}",
                result.OeePercentage, request.DeviceId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating OEE for device {DeviceId}", request.DeviceId);
            throw;
        }
    }
}
