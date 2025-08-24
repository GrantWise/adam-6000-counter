using Industrial.Adam.AdminDashboard.Domain.Entities;
using Industrial.Adam.AdminDashboard.Domain.Enums;

namespace Industrial.Adam.AdminDashboard.Domain.Interfaces;

/// <summary>
/// Repository interface for system health metrics
/// </summary>
public interface ISystemHealthMetricRepository
{
    /// <summary>
    /// Add a new health metric
    /// </summary>
    /// <param name="metric">The health metric to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The added metric</returns>
    Task<SystemHealthMetric> AddAsync(SystemHealthMetric metric, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the latest health metrics for all services
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of latest health metrics</returns>
    Task<IEnumerable<SystemHealthMetric>> GetLatestMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get health metrics for a specific service within a time range
    /// </summary>
    /// <param name="serviceName">Service name to filter by</param>
    /// <param name="fromTime">Start time range</param>
    /// <param name="toTime">End time range</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of health metrics for the service</returns>
    Task<IEnumerable<SystemHealthMetric>> GetServiceMetricsAsync(
        string serviceName, 
        DateTime fromTime, 
        DateTime toTime, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get services with specific status
    /// </summary>
    /// <param name="status">Status to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of services with the specified status</returns>
    Task<IEnumerable<SystemHealthMetric>> GetServicesByStatusAsync(
        ServiceStatus status, 
        CancellationToken cancellationToken = default);
}