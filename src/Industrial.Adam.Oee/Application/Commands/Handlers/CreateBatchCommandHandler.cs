using Industrial.Adam.Oee.Domain.Entities;
using Industrial.Adam.Oee.Domain.Interfaces;
using Industrial.Adam.Oee.Domain.Services;
using MediatR;

namespace Industrial.Adam.Oee.Application.Commands.Handlers;

/// <summary>
/// Handler for CreateBatchCommand
/// </summary>
public sealed class CreateBatchCommandHandler : IRequestHandler<CreateBatchCommand, CreateBatchResult>
{
    private readonly IWorkOrderRepository _workOrderRepository;
    private readonly IBatchRepository _batchRepository;
    private readonly BatchManagementService _batchManagementService;

    /// <summary>
    /// Initialize handler
    /// </summary>
    /// <param name="workOrderRepository">Work order repository</param>
    /// <param name="batchRepository">Batch repository</param>
    /// <param name="batchManagementService">Batch management service</param>
    public CreateBatchCommandHandler(
        IWorkOrderRepository workOrderRepository,
        IBatchRepository batchRepository,
        BatchManagementService batchManagementService)
    {
        _workOrderRepository = workOrderRepository ?? throw new ArgumentNullException(nameof(workOrderRepository));
        _batchRepository = batchRepository ?? throw new ArgumentNullException(nameof(batchRepository));
        _batchManagementService = batchManagementService ?? throw new ArgumentNullException(nameof(batchManagementService));
    }

    /// <summary>
    /// Handle CreateBatchCommand
    /// </summary>
    /// <param name="request">Command request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Create batch result</returns>
    public async Task<CreateBatchResult> Handle(CreateBatchCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate work order exists and supports batch tracking
            var workOrder = await _workOrderRepository.GetByIdAsync(request.WorkOrderId, cancellationToken);
            if (workOrder == null)
            {
                return new CreateBatchResult(false, null, $"Work order {request.WorkOrderId} not found");
            }

            if (!workOrder.BatchTrackingEnabled)
            {
                return new CreateBatchResult(false, null, $"Batch tracking is not enabled for work order {request.WorkOrderId}");
            }

            // Check if batch number already exists
            var existingBatch = await _batchRepository.GetByBatchNumberAsync(request.BatchNumber, cancellationToken);
            if (existingBatch != null)
            {
                return new CreateBatchResult(false, null, $"Batch number {request.BatchNumber} already exists");
            }

            // Create batch using domain service
            var batch = _batchManagementService.CreateBatch(
                workOrder,
                request.BatchNumber,
                request.OperatorId,
                request.EquipmentLineId,
                request.ParentBatchId,
                request.ShiftId
            );

            // Save batch to repository
            await _batchRepository.AddAsync(batch, cancellationToken);

            // Add batch to work order
            workOrder.AddBatch(batch.Id);
            await _workOrderRepository.UpdateAsync(workOrder, cancellationToken);

            return new CreateBatchResult(true, batch.Id, null);
        }
        catch (ArgumentException ex)
        {
            return new CreateBatchResult(false, null, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return new CreateBatchResult(false, null, ex.Message);
        }
        catch (Exception ex)
        {
            return new CreateBatchResult(false, null, $"An error occurred while creating the batch: {ex.Message}");
        }
    }
}
