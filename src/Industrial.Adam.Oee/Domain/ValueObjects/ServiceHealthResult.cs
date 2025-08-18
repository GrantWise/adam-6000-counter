namespace Industrial.Adam.Oee.Domain.ValueObjects;

/// <summary>
/// Value object representing the health status of an external service
/// </summary>
public sealed class ServiceHealthResult : ValueObject
{
    /// <summary>
    /// Name of the service being checked
    /// </summary>
    public string ServiceName { get; }

    /// <summary>
    /// Health status of the service
    /// </summary>
    public ServiceHealthStatus Status { get; }

    /// <summary>
    /// Response time for the health check
    /// </summary>
    public TimeSpan ResponseTime { get; }

    /// <summary>
    /// Timestamp when health check was performed
    /// </summary>
    public DateTime CheckedAt { get; }

    /// <summary>
    /// Optional error message if service is unhealthy
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Additional health check details
    /// </summary>
    public IReadOnlyDictionary<string, object>? Details { get; }

    /// <summary>
    /// Service version information
    /// </summary>
    public string? Version { get; }

    /// <summary>
    /// Creates a new ServiceHealthResult instance
    /// </summary>
    /// <param name="serviceName">Name of the service</param>
    /// <param name="status">Health status</param>
    /// <param name="responseTime">Response time</param>
    /// <param name="checkedAt">Time of health check</param>
    /// <param name="errorMessage">Error message (if unhealthy)</param>
    /// <param name="details">Additional details</param>
    /// <param name="version">Service version</param>
    public ServiceHealthResult(string serviceName, ServiceHealthStatus status, TimeSpan responseTime,
        DateTime? checkedAt = null, string? errorMessage = null, IReadOnlyDictionary<string, object>? details = null, string? version = null)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            throw new ArgumentException("Service name cannot be null or empty", nameof(serviceName));

        ServiceName = serviceName;
        Status = status;
        ResponseTime = responseTime;
        CheckedAt = checkedAt ?? DateTime.UtcNow;
        ErrorMessage = errorMessage;
        Details = details;
        Version = version;
    }

    /// <summary>
    /// Creates a healthy service result
    /// </summary>
    /// <param name="serviceName">Name of the service</param>
    /// <param name="responseTime">Response time</param>
    /// <param name="version">Service version</param>
    /// <param name="details">Additional details</param>
    /// <returns>Healthy service health result</returns>
    public static ServiceHealthResult Healthy(string serviceName, TimeSpan responseTime, string? version = null, IReadOnlyDictionary<string, object>? details = null)
    {
        return new ServiceHealthResult(serviceName, ServiceHealthStatus.Healthy, responseTime, version: version, details: details);
    }

    /// <summary>
    /// Creates a degraded service result
    /// </summary>
    /// <param name="serviceName">Name of the service</param>
    /// <param name="responseTime">Response time</param>
    /// <param name="warningMessage">Warning message</param>
    /// <param name="details">Additional details</param>
    /// <returns>Degraded service health result</returns>
    public static ServiceHealthResult Degraded(string serviceName, TimeSpan responseTime, string warningMessage, IReadOnlyDictionary<string, object>? details = null)
    {
        return new ServiceHealthResult(serviceName, ServiceHealthStatus.Degraded, responseTime, errorMessage: warningMessage, details: details);
    }

    /// <summary>
    /// Creates an unhealthy service result
    /// </summary>
    /// <param name="serviceName">Name of the service</param>
    /// <param name="responseTime">Response time</param>
    /// <param name="errorMessage">Error message</param>
    /// <param name="details">Additional details</param>
    /// <returns>Unhealthy service health result</returns>
    public static ServiceHealthResult Unhealthy(string serviceName, TimeSpan responseTime, string errorMessage, IReadOnlyDictionary<string, object>? details = null)
    {
        return new ServiceHealthResult(serviceName, ServiceHealthStatus.Unhealthy, responseTime, errorMessage: errorMessage, details: details);
    }

    /// <summary>
    /// Creates an unavailable service result
    /// </summary>
    /// <param name="serviceName">Name of the service</param>
    /// <param name="errorMessage">Error message</param>
    /// <returns>Unavailable service health result</returns>
    public static ServiceHealthResult Unavailable(string serviceName, string errorMessage)
    {
        return new ServiceHealthResult(serviceName, ServiceHealthStatus.Unavailable, TimeSpan.Zero, errorMessage: errorMessage);
    }

    /// <summary>
    /// Determines if the service is available for use
    /// </summary>
    /// <returns>True if service is healthy or degraded</returns>
    public bool IsAvailable() => Status == ServiceHealthStatus.Healthy || Status == ServiceHealthStatus.Degraded;

    /// <summary>
    /// Determines if the service is performing optimally
    /// </summary>
    /// <returns>True if service is healthy</returns>
    public bool IsOptimal() => Status == ServiceHealthStatus.Healthy;

    /// <summary>
    /// Gets the health score as a percentage (0.0 to 1.0)
    /// </summary>
    /// <returns>Health score</returns>
    public decimal GetHealthScore() => Status switch
    {
        ServiceHealthStatus.Healthy => 1.0m,
        ServiceHealthStatus.Degraded => 0.7m,
        ServiceHealthStatus.Unhealthy => 0.3m,
        ServiceHealthStatus.Unavailable => 0.0m,
        _ => 0.0m
    };

    /// <summary>
    /// Determines if the response time is acceptable
    /// </summary>
    /// <param name="threshold">Acceptable response time threshold</param>
    /// <returns>True if response time is within threshold</returns>
    public bool IsResponseTimeAcceptable(TimeSpan threshold) => ResponseTime <= threshold;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return ServiceName;
        yield return Status;
        yield return ResponseTime;
        yield return CheckedAt;

        if (ErrorMessage != null)
            yield return ErrorMessage;
        if (Version != null)
            yield return Version;

        if (Details != null)
        {
            foreach (var kvp in Details.OrderBy(x => x.Key))
            {
                yield return kvp.Key;
                yield return kvp.Value;
            }
        }
    }

    public override string ToString() =>
        $"ServiceHealth: {ServiceName} is {Status} (Response: {ResponseTime.TotalMilliseconds:F0}ms)";
}

/// <summary>
/// Enumeration of service health statuses
/// </summary>
public enum ServiceHealthStatus
{
    /// <summary>
    /// Service is fully operational
    /// </summary>
    Healthy = 0,

    /// <summary>
    /// Service is operational but with reduced performance
    /// </summary>
    Degraded = 1,

    /// <summary>
    /// Service is not functioning correctly
    /// </summary>
    Unhealthy = 2,

    /// <summary>
    /// Service is not available or not responding
    /// </summary>
    Unavailable = 3
}
