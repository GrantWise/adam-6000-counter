using Industrial.Adam.Logger.Health.Models;
using Industrial.Adam.Logger.Utilities;

namespace Industrial.Adam.Logger.Health;

/// <summary>
/// Service for performing comprehensive health checks on system components
/// </summary>
public interface IHealthCheckService
{
    /// <summary>
    /// Perform a comprehensive health check of all system components
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Complete health response with all component statuses</returns>
    Task<OperationResult<HealthResponse>> CheckHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check health of a specific component
    /// </summary>
    /// <param name="componentName">Name of the component to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health status of the specified component</returns>
    Task<OperationResult<ComponentHealth>> CheckComponentHealthAsync(string componentName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a quick health status without detailed metrics
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Quick health status for load balancer health checks</returns>
    Task<OperationResult<HealthStatus>> GetQuickHealthStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get detailed health metrics for monitoring systems
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed health metrics suitable for Prometheus or similar monitoring</returns>
    Task<OperationResult<IReadOnlyDictionary<string, object>>> GetHealthMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of available components that can be health checked
    /// </summary>
    /// <returns>List of component names that support health checking</returns>
    OperationResult<IReadOnlyList<string>> GetAvailableComponents();

    /// <summary>
    /// Register a custom health check for a component
    /// </summary>
    /// <param name="componentName">Name of the component</param>
    /// <param name="healthCheck">Health check function to execute</param>
    /// <param name="dependencies">Component dependencies</param>
    /// <returns>Success if registration was successful</returns>
    OperationResult RegisterComponentHealthCheck(
        string componentName,
        Func<CancellationToken, Task<ComponentHealth>> healthCheck,
        IReadOnlyList<string>? dependencies = null);

    /// <summary>
    /// Unregister a custom health check for a component
    /// </summary>
    /// <param name="componentName">Name of the component</param>
    /// <returns>Success if unregistration was successful</returns>
    OperationResult UnregisterComponentHealthCheck(string componentName);

    /// <summary>
    /// Event triggered when component health status changes
    /// </summary>
    event EventHandler<ComponentHealthChangedEventArgs> ComponentHealthChanged;

    /// <summary>
    /// Event triggered when overall system health status changes
    /// </summary>
    event EventHandler<SystemHealthChangedEventArgs> SystemHealthChanged;
}

/// <summary>
/// Event arguments for component health status changes
/// </summary>
public sealed class ComponentHealthChangedEventArgs : EventArgs
{
    /// <summary>
    /// Component name
    /// </summary>
    public required string ComponentName { get; init; }

    /// <summary>
    /// Previous health status
    /// </summary>
    public required HealthStatus PreviousStatus { get; init; }

    /// <summary>
    /// Current health status
    /// </summary>
    public required HealthStatus CurrentStatus { get; init; }

    /// <summary>
    /// Timestamp when the change occurred
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Current component health details
    /// </summary>
    public required ComponentHealth ComponentHealth { get; init; }
}

/// <summary>
/// Event arguments for system health status changes
/// </summary>
public sealed class SystemHealthChangedEventArgs : EventArgs
{
    /// <summary>
    /// Previous system health status
    /// </summary>
    public required HealthStatus PreviousStatus { get; init; }

    /// <summary>
    /// Current system health status
    /// </summary>
    public required HealthStatus CurrentStatus { get; init; }

    /// <summary>
    /// Previous system health score
    /// </summary>
    public required int PreviousHealthScore { get; init; }

    /// <summary>
    /// Current system health score
    /// </summary>
    public required int CurrentHealthScore { get; init; }

    /// <summary>
    /// Timestamp when the change occurred
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Current complete health response
    /// </summary>
    public required HealthResponse HealthResponse { get; init; }
}
