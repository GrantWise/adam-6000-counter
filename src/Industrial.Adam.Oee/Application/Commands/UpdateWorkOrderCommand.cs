using MediatR;

namespace Industrial.Adam.Oee.Application.Commands;

/// <summary>
/// Command to update work order details
/// </summary>
public class UpdateWorkOrderCommand : IRequest
{
    /// <summary>
    /// Work order identifier
    /// </summary>
    public string WorkOrderId { get; set; } = string.Empty;

    /// <summary>
    /// Updated work order description
    /// </summary>
    public string? WorkOrderDescription { get; set; }

    /// <summary>
    /// Updated planned quantity
    /// </summary>
    public decimal? PlannedQuantity { get; set; }

    /// <summary>
    /// Updated scheduled end time
    /// </summary>
    public DateTime? ScheduledEndTime { get; set; }

    /// <summary>
    /// Updated good quantity (from counter data)
    /// </summary>
    public decimal? ActualGoodQuantity { get; set; }

    /// <summary>
    /// Updated scrap quantity (from counter data)
    /// </summary>
    public decimal? ActualScrapQuantity { get; set; }

    /// <summary>
    /// Operator who made the update
    /// </summary>
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Constructor for creating command
    /// </summary>
    public UpdateWorkOrderCommand() { }

    /// <summary>
    /// Constructor with parameters
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    public UpdateWorkOrderCommand(string workOrderId)
    {
        WorkOrderId = workOrderId;
    }
}
