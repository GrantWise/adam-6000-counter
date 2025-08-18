using MediatR;

namespace Industrial.Adam.Oee.Application.Commands;

/// <summary>
/// Command to create a new batch
/// </summary>
/// <param name="BatchNumber">Batch number for identification</param>
/// <param name="WorkOrderId">Associated work order identifier</param>
/// <param name="PlannedQuantity">Planned quantity for the batch</param>
/// <param name="OperatorId">Operator creating the batch</param>
/// <param name="EquipmentLineId">Equipment line for production</param>
/// <param name="UnitOfMeasure">Unit of measure (default: pieces)</param>
/// <param name="ParentBatchId">Parent batch for genealogy (optional)</param>
/// <param name="ShiftId">Production shift (optional)</param>
public record CreateBatchCommand(
    string BatchNumber,
    string WorkOrderId,
    decimal PlannedQuantity,
    string OperatorId,
    string EquipmentLineId,
    string UnitOfMeasure = "pieces",
    string? ParentBatchId = null,
    string? ShiftId = null
) : IRequest<CreateBatchResult>;

/// <summary>
/// Result of creating a batch
/// </summary>
/// <param name="IsSuccess">Whether creation was successful</param>
/// <param name="BatchId">Created batch identifier</param>
/// <param name="ErrorMessage">Error message if creation failed</param>
public record CreateBatchResult(
    bool IsSuccess,
    string? BatchId,
    string? ErrorMessage
);
