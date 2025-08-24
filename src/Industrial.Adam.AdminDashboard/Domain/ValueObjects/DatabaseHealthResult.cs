using Industrial.Adam.AdminDashboard.Domain.Enums;

namespace Industrial.Adam.AdminDashboard.Domain.ValueObjects;

/// <summary>
/// Value object representing database health information
/// </summary>
public record DatabaseHealthResult
{
    /// <summary>
    /// Gets the database connection status
    /// </summary>
    public ServiceStatus Status { get; init; }

    /// <summary>
    /// Gets the connection response time in milliseconds
    /// </summary>
    public int ResponseTimeMs { get; init; }

    /// <summary>
    /// Gets the database version
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Gets the total database size in bytes
    /// </summary>
    public long DatabaseSizeBytes { get; init; }

    /// <summary>
    /// Gets the number of active connections
    /// </summary>
    public int ActiveConnections { get; init; }

    /// <summary>
    /// Gets the maximum allowed connections
    /// </summary>
    public int MaxConnections { get; init; }

    /// <summary>
    /// Gets the timestamp of the health check
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Gets the error message (if any)
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets additional database metrics
    /// </summary>
    public Dictionary<string, object>? AdditionalMetrics { get; init; }

    /// <summary>
    /// Gets the connection utilization percentage
    /// </summary>
    public double ConnectionUtilizationPercent => MaxConnections > 0 
        ? (double)ActiveConnections / MaxConnections * 100 
        : 0;

    /// <summary>
    /// Creates a healthy database health result
    /// </summary>
    /// <param name="responseTimeMs">Response time in milliseconds</param>
    /// <param name="version">Database version</param>
    /// <param name="databaseSizeBytes">Database size in bytes</param>
    /// <param name="activeConnections">Active connections</param>
    /// <param name="maxConnections">Maximum connections</param>
    /// <param name="additionalMetrics">Additional metrics</param>
    /// <returns>Healthy database health result</returns>
    public static DatabaseHealthResult Healthy(
        int responseTimeMs, 
        string? version = null, 
        long databaseSizeBytes = 0,
        int activeConnections = 0,
        int maxConnections = 0,
        Dictionary<string, object>? additionalMetrics = null)
    {
        return new DatabaseHealthResult
        {
            Status = ServiceStatus.Healthy,
            ResponseTimeMs = responseTimeMs,
            Version = version,
            DatabaseSizeBytes = databaseSizeBytes,
            ActiveConnections = activeConnections,
            MaxConnections = maxConnections,
            Timestamp = DateTime.UtcNow,
            AdditionalMetrics = additionalMetrics
        };
    }

    /// <summary>
    /// Creates an unhealthy database health result
    /// </summary>
    /// <param name="status">Database status</param>
    /// <param name="errorMessage">Error message</param>
    /// <param name="responseTimeMs">Response time in milliseconds</param>
    /// <returns>Unhealthy database health result</returns>
    public static DatabaseHealthResult Unhealthy(ServiceStatus status, string errorMessage, int responseTimeMs = 0)
    {
        return new DatabaseHealthResult
        {
            Status = status,
            ResponseTimeMs = responseTimeMs,
            ErrorMessage = errorMessage,
            Timestamp = DateTime.UtcNow
        };
    }
}