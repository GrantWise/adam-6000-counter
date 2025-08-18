using System.ComponentModel.DataAnnotations;

namespace Industrial.Adam.Oee.WebApi.Models;

/// <summary>
/// Request parameters for OEE history query
/// </summary>
public class OeeHistoryRequest
{
    /// <summary>
    /// Device/resource identifier
    /// </summary>
    [Required]
    [StringLength(20, MinimumLength = 1)]
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Number of hours to look back from current time
    /// </summary>
    [Range(1, 8760)] // 1 hour to 1 year
    public int Period { get; set; } = 24;

    /// <summary>
    /// Optional start time for custom date range
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// Optional end time for custom date range
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Data aggregation interval in minutes
    /// </summary>
    [Range(1, 1440)] // 1 minute to 24 hours
    public int IntervalMinutes { get; set; } = 60;
}
