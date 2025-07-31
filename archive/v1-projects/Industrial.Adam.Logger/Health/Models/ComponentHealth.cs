using Industrial.Adam.Logger.Health.Models;

namespace Industrial.Adam.Logger.Health.Models;

/// <summary>
/// Health status information for a specific system component
/// </summary>
public sealed record ComponentHealth
{
    /// <summary>
    /// Component name
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Health status of the component
    /// </summary>
    public required HealthStatus Status { get; init; }

    /// <summary>
    /// Component health score (0-100)
    /// </summary>
    public required int HealthScore { get; init; }

    /// <summary>
    /// Time taken to check component health
    /// </summary>
    public required TimeSpan CheckDuration { get; init; }

    /// <summary>
    /// Timestamp when component health was last checked
    /// </summary>
    public required DateTimeOffset LastChecked { get; init; }

    /// <summary>
    /// Component uptime since last restart
    /// </summary>
    public TimeSpan? Uptime { get; init; }

    /// <summary>
    /// Component-specific status message
    /// </summary>
    public string? StatusMessage { get; init; }

    /// <summary>
    /// Error message if component is unhealthy
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Component-specific metrics
    /// </summary>
    public required IReadOnlyDictionary<string, object> Metrics { get; init; }

    /// <summary>
    /// Component-specific warnings
    /// </summary>
    public required IReadOnlyList<string> Warnings { get; init; }

    /// <summary>
    /// Recommendations for improving component health
    /// </summary>
    public required IReadOnlyList<string> Recommendations { get; init; }

    /// <summary>
    /// Dependent components that affect this component's health
    /// </summary>
    public required IReadOnlyList<string> Dependencies { get; init; }

    /// <summary>
    /// Factory method for creating a healthy component
    /// </summary>
    /// <param name="name">Component name</param>
    /// <param name="checkDuration">Health check duration</param>
    /// <param name="statusMessage">Optional status message</param>
    /// <param name="metrics">Component metrics</param>
    /// <param name="uptime">Component uptime</param>
    /// <param name="dependencies">Component dependencies</param>
    /// <returns>Healthy component health instance</returns>
    public static ComponentHealth Healthy(
        string name,
        TimeSpan checkDuration,
        string? statusMessage = null,
        IReadOnlyDictionary<string, object>? metrics = null,
        TimeSpan? uptime = null,
        IReadOnlyList<string>? dependencies = null)
    {
        return new ComponentHealth
        {
            Name = name,
            Status = HealthStatus.Healthy,
            HealthScore = 100,
            CheckDuration = checkDuration,
            LastChecked = DateTimeOffset.UtcNow,
            Uptime = uptime,
            StatusMessage = statusMessage ?? "Component is operating normally",
            ErrorMessage = null,
            Metrics = metrics ?? new Dictionary<string, object>(),
            Warnings = Array.Empty<string>(),
            Recommendations = Array.Empty<string>(),
            Dependencies = dependencies ?? Array.Empty<string>()
        };
    }

    /// <summary>
    /// Factory method for creating a degraded component
    /// </summary>
    /// <param name="name">Component name</param>
    /// <param name="checkDuration">Health check duration</param>
    /// <param name="healthScore">Health score (0-100)</param>
    /// <param name="statusMessage">Status message</param>
    /// <param name="warnings">Component warnings</param>
    /// <param name="recommendations">Recommendations for improvement</param>
    /// <param name="metrics">Component metrics</param>
    /// <param name="uptime">Component uptime</param>
    /// <param name="dependencies">Component dependencies</param>
    /// <returns>Degraded component health instance</returns>
    public static ComponentHealth Degraded(
        string name,
        TimeSpan checkDuration,
        int healthScore,
        string statusMessage,
        IReadOnlyList<string> warnings,
        IReadOnlyList<string> recommendations,
        IReadOnlyDictionary<string, object>? metrics = null,
        TimeSpan? uptime = null,
        IReadOnlyList<string>? dependencies = null)
    {
        return new ComponentHealth
        {
            Name = name,
            Status = HealthStatus.Degraded,
            HealthScore = Math.Max(0, Math.Min(100, healthScore)),
            CheckDuration = checkDuration,
            LastChecked = DateTimeOffset.UtcNow,
            Uptime = uptime,
            StatusMessage = statusMessage,
            ErrorMessage = null,
            Metrics = metrics ?? new Dictionary<string, object>(),
            Warnings = warnings,
            Recommendations = recommendations,
            Dependencies = dependencies ?? Array.Empty<string>()
        };
    }

    /// <summary>
    /// Factory method for creating an unhealthy component
    /// </summary>
    /// <param name="name">Component name</param>
    /// <param name="checkDuration">Health check duration</param>
    /// <param name="errorMessage">Error message describing the issue</param>
    /// <param name="healthScore">Health score (0-100)</param>
    /// <param name="recommendations">Recommendations for fixing the issue</param>
    /// <param name="metrics">Component metrics</param>
    /// <param name="uptime">Component uptime</param>
    /// <param name="dependencies">Component dependencies</param>
    /// <returns>Unhealthy component health instance</returns>
    public static ComponentHealth Unhealthy(
        string name,
        TimeSpan checkDuration,
        string errorMessage,
        int healthScore = 0,
        IReadOnlyList<string>? recommendations = null,
        IReadOnlyDictionary<string, object>? metrics = null,
        TimeSpan? uptime = null,
        IReadOnlyList<string>? dependencies = null)
    {
        return new ComponentHealth
        {
            Name = name,
            Status = HealthStatus.Unhealthy,
            HealthScore = Math.Max(0, Math.Min(100, healthScore)),
            CheckDuration = checkDuration,
            LastChecked = DateTimeOffset.UtcNow,
            Uptime = uptime,
            StatusMessage = null,
            ErrorMessage = errorMessage,
            Metrics = metrics ?? new Dictionary<string, object>(),
            Warnings = Array.Empty<string>(),
            Recommendations = recommendations ?? new[] { "Investigate and resolve the underlying issue" },
            Dependencies = dependencies ?? Array.Empty<string>()
        };
    }

    /// <summary>
    /// Factory method for creating a critical component
    /// </summary>
    /// <param name="name">Component name</param>
    /// <param name="checkDuration">Health check duration</param>
    /// <param name="errorMessage">Critical error message</param>
    /// <param name="recommendations">Urgent recommendations for fixing the issue</param>
    /// <param name="metrics">Component metrics</param>
    /// <param name="dependencies">Component dependencies</param>
    /// <returns>Critical component health instance</returns>
    public static ComponentHealth Critical(
        string name,
        TimeSpan checkDuration,
        string errorMessage,
        IReadOnlyList<string>? recommendations = null,
        IReadOnlyDictionary<string, object>? metrics = null,
        IReadOnlyList<string>? dependencies = null)
    {
        return new ComponentHealth
        {
            Name = name,
            Status = HealthStatus.Critical,
            HealthScore = 0,
            CheckDuration = checkDuration,
            LastChecked = DateTimeOffset.UtcNow,
            Uptime = null,
            StatusMessage = null,
            ErrorMessage = errorMessage,
            Metrics = metrics ?? new Dictionary<string, object>(),
            Warnings = Array.Empty<string>(),
            Recommendations = recommendations ?? new[] { "Immediate attention required - system may be non-functional" },
            Dependencies = dependencies ?? Array.Empty<string>()
        };
    }
}
