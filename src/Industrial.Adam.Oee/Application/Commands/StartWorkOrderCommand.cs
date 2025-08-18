using MediatR;

namespace Industrial.Adam.Oee.Application.Commands;

/// <summary>
/// Command to start a new work order
/// </summary>
public class StartWorkOrderCommand : IRequest<string>
{
    /// <summary>
    /// Work order identifier
    /// </summary>
    public string WorkOrderId { get; set; } = string.Empty;

    /// <summary>
    /// Work order description
    /// </summary>
    public string WorkOrderDescription { get; set; } = string.Empty;

    /// <summary>
    /// Product identifier
    /// </summary>
    public string ProductId { get; set; } = string.Empty;

    /// <summary>
    /// Product description
    /// </summary>
    public string ProductDescription { get; set; } = string.Empty;

    /// <summary>
    /// Planned quantity to produce
    /// </summary>
    public decimal PlannedQuantity { get; set; }

    /// <summary>
    /// Unit of measure for quantities
    /// </summary>
    public string UnitOfMeasure { get; set; } = "pieces";

    /// <summary>
    /// Scheduled start time
    /// </summary>
    public DateTime ScheduledStartTime { get; set; }

    /// <summary>
    /// Scheduled end time
    /// </summary>
    public DateTime ScheduledEndTime { get; set; }

    /// <summary>
    /// Equipment line identifier
    /// </summary>
    public string LineId { get; set; } = string.Empty;

    /// <summary>
    /// Operator identifier
    /// </summary>
    public string? OperatorId { get; set; }

    /// <summary>
    /// Constructor for creating command
    /// </summary>
    public StartWorkOrderCommand() { }

    /// <summary>
    /// Constructor with parameters
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="workOrderDescription">Work order description</param>
    /// <param name="productId">Product identifier</param>
    /// <param name="productDescription">Product description</param>
    /// <param name="plannedQuantity">Planned quantity</param>
    /// <param name="scheduledStartTime">Scheduled start time</param>
    /// <param name="scheduledEndTime">Scheduled end time</param>
    /// <param name="lineId">Equipment line identifier</param>
    /// <param name="unitOfMeasure">Unit of measure</param>
    /// <param name="operatorId">Operator identifier</param>
    public StartWorkOrderCommand(
        string workOrderId,
        string workOrderDescription,
        string productId,
        string productDescription,
        decimal plannedQuantity,
        DateTime scheduledStartTime,
        DateTime scheduledEndTime,
        string lineId,
        string unitOfMeasure = "pieces",
        string? operatorId = null)
    {
        WorkOrderId = workOrderId;
        WorkOrderDescription = workOrderDescription;
        ProductId = productId;
        ProductDescription = productDescription;
        PlannedQuantity = plannedQuantity;
        UnitOfMeasure = unitOfMeasure;
        ScheduledStartTime = scheduledStartTime;
        ScheduledEndTime = scheduledEndTime;
        LineId = lineId;
        OperatorId = operatorId;
    }
}
