using System.ComponentModel.DataAnnotations;

namespace Industrial.Adam.Oee.WebApi.Models;

/// <summary>
/// Request model for starting a new work order
/// </summary>
public class StartWorkOrderRequest
{
    /// <summary>
    /// Work order identifier
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string WorkOrderId { get; set; } = string.Empty;

    /// <summary>
    /// Work order description
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string WorkOrderDescription { get; set; } = string.Empty;

    /// <summary>
    /// Product identifier
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string ProductId { get; set; } = string.Empty;

    /// <summary>
    /// Product description
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string ProductDescription { get; set; } = string.Empty;

    /// <summary>
    /// Planned quantity to produce
    /// </summary>
    [Required]
    [Range(0.1, 999999.99)]
    public decimal PlannedQuantity { get; set; }

    /// <summary>
    /// Unit of measure for quantities
    /// </summary>
    [StringLength(20)]
    public string UnitOfMeasure { get; set; } = "pieces";

    /// <summary>
    /// Scheduled start time
    /// </summary>
    [Required]
    public DateTime ScheduledStartTime { get; set; }

    /// <summary>
    /// Scheduled end time
    /// </summary>
    [Required]
    public DateTime ScheduledEndTime { get; set; }

    /// <summary>
    /// Device/resource identifier
    /// </summary>
    [Required]
    [StringLength(20, MinimumLength = 1)]
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Operator identifier
    /// </summary>
    [StringLength(50)]
    public string? OperatorId { get; set; }
}
