using MediatR;

namespace Industrial.Adam.Oee.Application.Commands;

/// <summary>
/// Command to complete an active work order
/// </summary>
public class CompleteWorkOrderCommand : IRequest
{
    /// <summary>
    /// Work order identifier
    /// </summary>
    public string WorkOrderId { get; set; } = string.Empty;

    /// <summary>
    /// Optional reason for completion
    /// </summary>
    public string? CompletionReason { get; set; }

    /// <summary>
    /// Final good quantity produced
    /// </summary>
    public decimal? FinalGoodQuantity { get; set; }

    /// <summary>
    /// Final scrap quantity produced
    /// </summary>
    public decimal? FinalScrapQuantity { get; set; }

    /// <summary>
    /// Operator who completed the work order
    /// </summary>
    public string? CompletedBy { get; set; }

    /// <summary>
    /// Constructor for creating command
    /// </summary>
    public CompleteWorkOrderCommand() { }

    /// <summary>
    /// Constructor with parameters
    /// </summary>
    /// <param name="workOrderId">Work order identifier</param>
    /// <param name="completionReason">Optional completion reason</param>
    /// <param name="finalGoodQuantity">Final good quantity</param>
    /// <param name="finalScrapQuantity">Final scrap quantity</param>
    /// <param name="completedBy">Operator who completed</param>
    public CompleteWorkOrderCommand(
        string workOrderId,
        string? completionReason = null,
        decimal? finalGoodQuantity = null,
        decimal? finalScrapQuantity = null,
        string? completedBy = null)
    {
        WorkOrderId = workOrderId;
        CompletionReason = completionReason;
        FinalGoodQuantity = finalGoodQuantity;
        FinalScrapQuantity = finalScrapQuantity;
        CompletedBy = completedBy;
    }
}
