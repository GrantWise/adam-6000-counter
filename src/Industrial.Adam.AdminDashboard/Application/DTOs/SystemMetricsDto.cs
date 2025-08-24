namespace Industrial.Adam.AdminDashboard.Application.DTOs;

/// <summary>
/// DTO for system performance metrics
/// </summary>
public class SystemMetricsDto
{
    /// <summary>
    /// Gets or sets the CPU usage percentage
    /// </summary>
    public double CpuUsagePercent { get; set; }

    /// <summary>
    /// Gets or sets the memory usage in bytes
    /// </summary>
    public long MemoryUsageBytes { get; set; }

    /// <summary>
    /// Gets or sets the total memory in bytes
    /// </summary>
    public long TotalMemoryBytes { get; set; }

    /// <summary>
    /// Gets or sets the disk usage in bytes
    /// </summary>
    public long DiskUsageBytes { get; set; }

    /// <summary>
    /// Gets or sets the total disk space in bytes
    /// </summary>
    public long TotalDiskBytes { get; set; }

    /// <summary>
    /// Gets or sets the timestamp
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the system uptime in seconds
    /// </summary>
    public long UptimeSeconds { get; set; }

    /// <summary>
    /// Gets or sets additional metrics
    /// </summary>
    public Dictionary<string, object>? AdditionalMetrics { get; set; }
}