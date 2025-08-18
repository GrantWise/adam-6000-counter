using System.ComponentModel.DataAnnotations;

namespace Industrial.Adam.Oee.WebApi.Models;

/// <summary>
/// Request model for completing a work order
/// </summary>
public class CompleteWorkOrderRequest
{
    /// <summary>
    /// Actual quantity of good pieces produced
    /// </summary>
    [Range(0, 999999.99)]
    public decimal ActualQuantityGood { get; set; }

    /// <summary>
    /// Actual quantity of scrap/defective pieces
    /// </summary>
    [Range(0, 999999.99)]
    public decimal ActualQuantityScrap { get; set; }

    /// <summary>
    /// Operator identifier who completed the work
    /// </summary>
    [StringLength(50)]
    public string? CompletedByOperatorId { get; set; }

    /// <summary>
    /// Optional completion notes
    /// </summary>
    [StringLength(500)]
    public string? CompletionNotes { get; set; }

    /// <summary>
    /// Actual completion time (defaults to current time if not provided)
    /// </summary>
    public DateTime? ActualEndTime { get; set; }
}
