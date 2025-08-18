namespace Industrial.Adam.Oee.Application.DTOs;

/// <summary>
/// Data transfer object for OEE metrics
/// </summary>
public class OeeMetricsDto
{
    /// <summary>
    /// Availability percentage (0-100)
    /// </summary>
    public decimal AvailabilityPercent { get; set; }

    /// <summary>
    /// Performance percentage (0-100)
    /// </summary>
    public decimal PerformancePercent { get; set; }

    /// <summary>
    /// Quality percentage (0-100)
    /// </summary>
    public decimal QualityPercent { get; set; }

    /// <summary>
    /// Overall OEE percentage (0-100)
    /// </summary>
    public decimal OeePercent { get; set; }

    /// <summary>
    /// Current production rate (units per minute)
    /// </summary>
    public decimal CurrentRate { get; set; }

    /// <summary>
    /// Target production rate (units per minute)
    /// </summary>
    public decimal TargetRate { get; set; }

    /// <summary>
    /// Current operational status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of last data update
    /// </summary>
    public DateTime LastUpdate { get; set; }

    /// <summary>
    /// Factor with the worst performance (Availability, Performance, or Quality)
    /// </summary>
    public string WorstFactor { get; set; } = string.Empty;

    /// <summary>
    /// Whether the OEE metrics require operator attention
    /// </summary>
    public bool RequiresAttention { get; set; }

    /// <summary>
    /// Device or resource identifier
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Current job number if active
    /// </summary>
    public string? JobNumber { get; set; }

    /// <summary>
    /// Calculation period start time
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// Calculation period end time
    /// </summary>
    public DateTime PeriodEnd { get; set; }
}
