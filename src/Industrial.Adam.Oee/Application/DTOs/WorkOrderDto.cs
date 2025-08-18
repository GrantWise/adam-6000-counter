using Industrial.Adam.Oee.Domain.Entities;

namespace Industrial.Adam.Oee.Application.DTOs;

/// <summary>
/// Data transfer object for work order information
/// </summary>
public class WorkOrderDto
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
    public string UnitOfMeasure { get; set; } = string.Empty;

    /// <summary>
    /// Scheduled start time
    /// </summary>
    public DateTime ScheduledStartTime { get; set; }

    /// <summary>
    /// Scheduled end time
    /// </summary>
    public DateTime ScheduledEndTime { get; set; }

    /// <summary>
    /// Resource reference (device/machine identifier)
    /// </summary>
    public string ResourceReference { get; set; } = string.Empty;

    /// <summary>
    /// Current work order status
    /// </summary>
    public string Status { get; set; } = string.Empty;

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
    /// Actual start time (when work began)
    /// </summary>
    public DateTime? ActualStartTime { get; set; }

    /// <summary>
    /// Actual end time (when work completed)
    /// </summary>
    public DateTime? ActualEndTime { get; set; }

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
    /// When this work order was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Create DTO from domain entity
    /// </summary>
    /// <param name="workOrder">Work order domain entity</param>
    /// <returns>Work order DTO</returns>
    public static WorkOrderDto FromDomain(WorkOrder workOrder)
    {
        return new WorkOrderDto
        {
            WorkOrderId = workOrder.Id,
            WorkOrderDescription = workOrder.WorkOrderDescription,
            ProductId = workOrder.ProductId,
            ProductDescription = workOrder.ProductDescription,
            PlannedQuantity = workOrder.PlannedQuantity,
            UnitOfMeasure = workOrder.UnitOfMeasure,
            ScheduledStartTime = workOrder.ScheduledStartTime,
            ScheduledEndTime = workOrder.ScheduledEndTime,
            ResourceReference = workOrder.ResourceReference,
            Status = workOrder.Status.ToString(),
            ActualQuantityGood = workOrder.ActualQuantityGood,
            ActualQuantityScrap = workOrder.ActualQuantityScrap,
            TotalQuantityProduced = workOrder.TotalQuantityProduced,
            ActualStartTime = workOrder.ActualStartTime,
            ActualEndTime = workOrder.ActualEndTime,
            CompletionPercentage = workOrder.GetCompletionPercentage(),
            YieldPercentage = workOrder.GetYieldPercentage(),
            ProductionRate = workOrder.GetProductionRate(),
            IsBehindSchedule = workOrder.IsBehindSchedule(),
            RequiresAttention = workOrder.RequiresAttention(),
            EstimatedCompletionTime = workOrder.GetEstimatedCompletionTime(),
            CreatedAt = workOrder.CreatedAt,
            UpdatedAt = workOrder.UpdatedAt
        };
    }
}
