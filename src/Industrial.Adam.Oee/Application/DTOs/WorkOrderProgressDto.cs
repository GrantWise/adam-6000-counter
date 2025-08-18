namespace Industrial.Adam.Oee.Application.DTOs;

/// <summary>
/// Data transfer object for work order progress metrics
/// </summary>
public class WorkOrderProgressDto
{
    /// <summary>
    /// Work order identifier
    /// </summary>
    public string WorkOrderId { get; set; } = string.Empty;

    /// <summary>
    /// Product description
    /// </summary>
    public string ProductDescription { get; set; } = string.Empty;

    /// <summary>
    /// Current work order status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Completion percentage based on planned quantity
    /// </summary>
    public decimal CompletionPercentage { get; set; }

    /// <summary>
    /// Yield/quality percentage
    /// </summary>
    public decimal YieldPercentage { get; set; }

    /// <summary>
    /// Production rate (pieces per minute)
    /// </summary>
    public decimal ProductionRate { get; set; }

    /// <summary>
    /// Planned quantity to produce
    /// </summary>
    public decimal PlannedQuantity { get; set; }

    /// <summary>
    /// Actual quantity of good pieces produced
    /// </summary>
    public decimal ActualQuantityGood { get; set; }

    /// <summary>
    /// Actual quantity of scrap/defective pieces
    /// </summary>
    public decimal ActualQuantityScrap { get; set; }

    /// <summary>
    /// Total quantity produced (good + scrap)
    /// </summary>
    public decimal TotalQuantityProduced { get; set; }

    /// <summary>
    /// Whether work order is behind schedule
    /// </summary>
    public bool IsBehindSchedule { get; set; }

    /// <summary>
    /// Whether work order requires attention
    /// </summary>
    public bool RequiresAttention { get; set; }

    /// <summary>
    /// Estimated completion time
    /// </summary>
    public DateTime? EstimatedCompletionTime { get; set; }

    /// <summary>
    /// Actual start time
    /// </summary>
    public DateTime? ActualStartTime { get; set; }

    /// <summary>
    /// Scheduled end time
    /// </summary>
    public DateTime ScheduledEndTime { get; set; }

    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime LastUpdate { get; set; }
}
