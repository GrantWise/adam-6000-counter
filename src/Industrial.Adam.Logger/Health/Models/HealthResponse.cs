using Industrial.Adam.Logger.Models;

namespace Industrial.Adam.Logger.Health.Models;

/// <summary>
/// Health check response with comprehensive system status information
/// </summary>
public sealed record HealthResponse
{
    /// <summary>
    /// Overall system health status
    /// </summary>
    public required HealthStatus Status { get; init; }

    /// <summary>
    /// Overall health score (0-100)
    /// </summary>
    public required int HealthScore { get; init; }

    /// <summary>
    /// Timestamp when health check was performed
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Total time taken for health check
    /// </summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// System uptime
    /// </summary>
    public required TimeSpan Uptime { get; init; }

    /// <summary>
    /// Health status of individual components
    /// </summary>
    public required IReadOnlyDictionary<string, ComponentHealth> Components { get; init; }

    /// <summary>
    /// System-wide metrics and statistics
    /// </summary>
    public required IReadOnlyDictionary<string, object> Metrics { get; init; }

    /// <summary>
    /// Health check warnings that don't affect overall status
    /// </summary>
    public required IReadOnlyList<string> Warnings { get; init; }

    /// <summary>
    /// Recommendations for improving system health
    /// </summary>
    public required IReadOnlyList<string> Recommendations { get; init; }

    /// <summary>
    /// Version information for the system
    /// </summary>
    public required VersionInfo Version { get; init; }

    /// <summary>
    /// Environment information
    /// </summary>
    public required EnvironmentInfo Environment { get; init; }
}

/// <summary>
/// Health status enumeration
/// </summary>
public enum HealthStatus
{
    /// <summary>
    /// System is healthy and operating normally
    /// </summary>
    Healthy = 0,

    /// <summary>
    /// System is operational but has some degraded components
    /// </summary>
    Degraded = 1,

    /// <summary>
    /// System has significant issues but is still functional
    /// </summary>
    Unhealthy = 2,

    /// <summary>
    /// System is critically unhealthy and may not be functional
    /// </summary>
    Critical = 3
}

/// <summary>
/// Version information for the system
/// </summary>
public sealed record VersionInfo
{
    /// <summary>
    /// Application version
    /// </summary>
    public required string ApplicationVersion { get; init; }

    /// <summary>
    /// .NET runtime version
    /// </summary>
    public required string RuntimeVersion { get; init; }

    /// <summary>
    /// Build timestamp
    /// </summary>
    public required DateTimeOffset BuildTimestamp { get; init; }

    /// <summary>
    /// Git commit hash (if available)
    /// </summary>
    public string? GitCommitHash { get; init; }

    /// <summary>
    /// Git branch name (if available)
    /// </summary>
    public string? GitBranch { get; init; }
}

/// <summary>
/// Environment information
/// </summary>
public sealed record EnvironmentInfo
{
    /// <summary>
    /// Environment name (Development, Staging, Production)
    /// </summary>
    public required string EnvironmentName { get; init; }

    /// <summary>
    /// Machine name where the application is running
    /// </summary>
    public required string MachineName { get; init; }

    /// <summary>
    /// Operating system description
    /// </summary>
    public required string OperatingSystem { get; init; }

    /// <summary>
    /// Process ID
    /// </summary>
    public required int ProcessId { get; init; }

    /// <summary>
    /// Processor count
    /// </summary>
    public required int ProcessorCount { get; init; }

    /// <summary>
    /// Total system memory in bytes
    /// </summary>
    public required long TotalMemoryBytes { get; init; }

    /// <summary>
    /// Available system memory in bytes
    /// </summary>
    public required long AvailableMemoryBytes { get; init; }
}
