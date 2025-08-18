using Industrial.Adam.Oee.Application.Commands;
using Industrial.Adam.Oee.Domain.Entities;
using Industrial.Adam.Oee.Domain.Interfaces;
using Industrial.Adam.Oee.Domain.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Application.Commands.Handlers;

/// <summary>
/// Handler for StartWorkOrderCommand
/// </summary>
public class StartWorkOrderCommandHandler : IRequestHandler<StartWorkOrderCommand, string>
{
    private readonly IWorkOrderRepository _workOrderRepository;
    private readonly ICounterDataRepository _counterDataRepository;
    private readonly IJobSequencingService _jobSequencingService;
    private readonly IEquipmentLineService _equipmentLineService;
    private readonly ILogger<StartWorkOrderCommandHandler> _logger;

    public StartWorkOrderCommandHandler(
        IWorkOrderRepository workOrderRepository,
        ICounterDataRepository counterDataRepository,
        IJobSequencingService jobSequencingService,
        IEquipmentLineService equipmentLineService,
        ILogger<StartWorkOrderCommandHandler> logger)
    {
        _workOrderRepository = workOrderRepository;
        _counterDataRepository = counterDataRepository;
        _jobSequencingService = jobSequencingService;
        _equipmentLineService = equipmentLineService;
        _logger = logger;
    }

    public async Task<string> Handle(StartWorkOrderCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting work order {WorkOrderId} for equipment line {LineId}",
            request.WorkOrderId, request.LineId);

        try
        {
            // Validate job sequencing rules
            var sequencingValidation = await _jobSequencingService.ValidateJobStartAsync(
                request.LineId, request.WorkOrderId, cancellationToken);

            if (!sequencingValidation.IsValid)
            {
                _logger.LogWarning("Job sequencing validation failed for work order {WorkOrderId} on line {LineId}: {Error}",
                    request.WorkOrderId, request.LineId, sequencingValidation.ErrorMessage);

                throw new InvalidOperationException(sequencingValidation.ErrorMessage);
            }

            // Validate equipment assignment
            var equipmentValidation = await _equipmentLineService.ValidateWorkOrderEquipmentAsync(
                request.WorkOrderId, request.LineId, cancellationToken);

            if (!equipmentValidation.IsValid)
            {
                _logger.LogWarning("Equipment validation failed for work order {WorkOrderId} on line {LineId}: {Error}",
                    request.WorkOrderId, request.LineId, equipmentValidation.ErrorMessage);

                throw new InvalidOperationException(equipmentValidation.ErrorMessage);
            }

            // Get equipment line for ADAM device mapping
            var equipmentLine = equipmentValidation.EquipmentLine!;

            // Get current counter snapshot for initializing quantities
            var counterSnapshot = await GetCurrentCounterSnapshotAsync(equipmentLine.AdamDeviceId, cancellationToken);

            // Create work order creation data
            var workOrderData = new WorkOrderCreationData(
                request.WorkOrderId,
                request.WorkOrderDescription,
                request.ProductId,
                request.ProductDescription,
                request.PlannedQuantity,
                request.ScheduledStartTime,
                request.ScheduledEndTime,
                equipmentLine.AdamDeviceId, // Use ADAM device ID from equipment line
                request.UnitOfMeasure
            );

            // Create the work order (already active from counter snapshot)
            var workOrder = WorkOrder.FromCounterSnapshot(workOrderData, counterSnapshot);

            // Save to repository
            var workOrderId = await _workOrderRepository.CreateAsync(workOrder, cancellationToken);

            _logger.LogInformation("Successfully started work order {WorkOrderId} for equipment line {LineId} (ADAM device {AdamDeviceId})",
                workOrderId, request.LineId, equipmentLine.AdamDeviceId);

            return workOrderId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting work order {WorkOrderId} for equipment line {LineId}",
                request.WorkOrderId, request.LineId);
            throw;
        }
    }

    /// <summary>
    /// Get current counter snapshot for initializing work order quantities
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current counter snapshot</returns>
    private async Task<CounterSnapshot> GetCurrentCounterSnapshotAsync(string deviceId, CancellationToken cancellationToken)
    {
        try
        {
            // Get the latest counter data for both channels
            var latestChannel0 = await _counterDataRepository.GetLatestReadingAsync(deviceId, 0, cancellationToken);
            var latestChannel1 = await _counterDataRepository.GetLatestReadingAsync(deviceId, 1, cancellationToken);

            if (latestChannel0 != null || latestChannel1 != null)
            {
                return new CounterSnapshot(
                    latestChannel0?.ProcessedValue ?? 0,
                    latestChannel1?.ProcessedValue ?? 0
                );
            }

            // If no counter data exists, start with zeros
            _logger.LogWarning("No counter data found for device {DeviceId}, starting with zero counts", deviceId);
            return new CounterSnapshot(0, 0);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting counter snapshot for device {DeviceId}, using zero counts", deviceId);
            return new CounterSnapshot(0, 0);
        }
    }
}
