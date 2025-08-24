using Industrial.Adam.AdminDashboard.Domain.ValueObjects;

namespace Industrial.Adam.AdminDashboard.Domain.Interfaces;

/// <summary>
/// Service interface for system health monitoring
/// </summary>
public interface ISystemHealthService
{
    /// <summary>
    /// Check health of all registered services
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of service health results</returns>
    Task<IEnumerable<ServiceHealthResult>> CheckAllServicesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check health of a specific service
    /// </summary>
    /// <param name="serviceName">Name of the service to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Service health result</returns>
    Task<ServiceHealthResult> CheckServiceHealthAsync(string serviceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get system performance metrics
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>System metrics</returns>
    Task<SystemMetrics> GetSystemMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get database health information
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Database health result</returns>
    Task<DatabaseHealthResult> GetDatabaseHealthAsync(CancellationToken cancellationToken = default);
}