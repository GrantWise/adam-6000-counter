namespace Industrial.Adam.AdminDashboard.Domain.ValueObjects;

/// <summary>
/// Value object representing system performance metrics
/// </summary>
public record SystemMetrics
{
    /// <summary>
    /// Gets the CPU usage percentage
    /// </summary>
    public double CpuUsagePercent { get; init; }

    /// <summary>
    /// Gets the memory usage in bytes
    /// </summary>
    public long MemoryUsageBytes { get; init; }

    /// <summary>
    /// Gets the total memory in bytes
    /// </summary>
    public long TotalMemoryBytes { get; init; }

    /// <summary>
    /// Gets the disk usage in bytes
    /// </summary>
    public long DiskUsageBytes { get; init; }

    /// <summary>
    /// Gets the total disk space in bytes
    /// </summary>
    public long TotalDiskBytes { get; init; }

    /// <summary>
    /// Gets the timestamp when metrics were collected
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Gets the system uptime in seconds
    /// </summary>
    public long UptimeSeconds { get; init; }

    /// <summary>
    /// Gets additional system metrics
    /// </summary>
    public Dictionary<string, object>? AdditionalMetrics { get; init; }

    /// <summary>
    /// Gets the memory usage percentage
    /// </summary>
    public double MemoryUsagePercent => TotalMemoryBytes > 0 
        ? (double)MemoryUsageBytes / TotalMemoryBytes * 100 
        : 0;

    /// <summary>
    /// Gets the disk usage percentage
    /// </summary>
    public double DiskUsagePercent => TotalDiskBytes > 0 
        ? (double)DiskUsageBytes / TotalDiskBytes * 100 
        : 0;

    /// <summary>
    /// Gets the available memory in bytes
    /// </summary>
    public long AvailableMemoryBytes => TotalMemoryBytes - MemoryUsageBytes;

    /// <summary>
    /// Gets the available disk space in bytes
    /// </summary>
    public long AvailableDiskBytes => TotalDiskBytes - DiskUsageBytes;
}