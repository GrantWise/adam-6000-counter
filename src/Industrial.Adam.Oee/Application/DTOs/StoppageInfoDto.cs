namespace Industrial.Adam.Oee.Application.DTOs;

/// <summary>
/// Data transfer object for stoppage information
/// </summary>
public class StoppageInfoDto
{
    /// <summary>
    /// When the stoppage started
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Duration of the stoppage in minutes
    /// </summary>
    public decimal DurationMinutes { get; set; }

    /// <summary>
    /// Whether the stoppage is currently active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Device/resource identifier
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Estimated production impact of the stoppage
    /// </summary>
    public StoppageImpactDto? EstimatedImpact { get; set; }

    /// <summary>
    /// Stoppage classification (if available)
    /// </summary>
    public string? Classification { get; set; }

    /// <summary>
    /// When the stoppage ended (null if still active)
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Calculated end time based on start time and duration
    /// </summary>
    public DateTime CalculatedEndTime => StartTime.AddMinutes((double)DurationMinutes);
}

/// <summary>
/// Data transfer object for stoppage impact information
/// </summary>
public class StoppageImpactDto
{
    /// <summary>
    /// Estimated units of production lost
    /// </summary>
    public decimal LostProductionUnits { get; set; }

    /// <summary>
    /// Estimated revenue impact (if available)
    /// </summary>
    public decimal? LostRevenue { get; set; }

    /// <summary>
    /// Impact on availability percentage
    /// </summary>
    public decimal AvailabilityImpact { get; set; }
}
