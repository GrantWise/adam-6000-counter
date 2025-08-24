using Industrial.Adam.AdminDashboard.Domain.Enums;

namespace Industrial.Adam.AdminDashboard.Domain.ValueObjects;

/// <summary>
/// Value object representing the health check result of a service
/// </summary>
public record ServiceHealthResult
{
    /// <summary>
    /// Gets the service name
    /// </summary>
    public required string ServiceName { get; init; }

    /// <summary>
    /// Gets the service status
    /// </summary>
    public ServiceStatus Status { get; init; }

    /// <summary>
    /// Gets the response time in milliseconds
    /// </summary>
    public int ResponseTimeMs { get; init; }

    /// <summary>
    /// Gets the error count
    /// </summary>
    public int ErrorCount { get; init; }

    /// <summary>
    /// Gets the health check timestamp
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Gets the error message (if any)
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets additional health data
    /// </summary>
    public Dictionary<string, object>? AdditionalData { get; init; }

    /// <summary>
    /// Creates a healthy service health result
    /// </summary>
    /// <param name="serviceName">Service name</param>
    /// <param name="responseTimeMs">Response time in milliseconds</param>
    /// <param name="additionalData">Additional data</param>
    /// <returns>Healthy service health result</returns>
    public static ServiceHealthResult Healthy(string serviceName, int responseTimeMs, Dictionary<string, object>? additionalData = null)
    {
        return new ServiceHealthResult
        {
            ServiceName = serviceName,
            Status = ServiceStatus.Healthy,
            ResponseTimeMs = responseTimeMs,
            ErrorCount = 0,
            Timestamp = DateTime.UtcNow,
            AdditionalData = additionalData
        };
    }

    /// <summary>
    /// Creates an unhealthy service health result
    /// </summary>
    /// <param name="serviceName">Service name</param>
    /// <param name="status">Service status</param>
    /// <param name="errorMessage">Error message</param>
    /// <param name="errorCount">Error count</param>
    /// <param name="responseTimeMs">Response time in milliseconds</param>
    /// <returns>Unhealthy service health result</returns>
    public static ServiceHealthResult Unhealthy(string serviceName, ServiceStatus status, string errorMessage, int errorCount = 1, int responseTimeMs = 0)
    {
        return new ServiceHealthResult
        {
            ServiceName = serviceName,
            Status = status,
            ResponseTimeMs = responseTimeMs,
            ErrorCount = errorCount,
            ErrorMessage = errorMessage,
            Timestamp = DateTime.UtcNow
        };
    }
}