namespace Industrial.Adam.AdminDashboard.Domain.Interfaces;

/// <summary>
/// Service interface for broadcasting health updates via SignalR
/// </summary>
public interface IHealthUpdateService
{
    /// <summary>
    /// Broadcast system health metrics to connected clients
    /// </summary>
    /// <param name="healthData">Health data to broadcast</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task BroadcastSystemHealthAsync(object healthData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcast system performance metrics to connected clients
    /// </summary>
    /// <param name="metrics">System metrics to broadcast</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task BroadcastSystemMetricsAsync(object metrics, CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcast system alerts to connected clients
    /// </summary>
    /// <param name="alerts">Active alerts to broadcast</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task BroadcastSystemAlertsAsync(object alerts, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send health update to specific user group
    /// </summary>
    /// <param name="groupName">Target group name</param>
    /// <param name="healthData">Health data to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendToGroupAsync(string groupName, object healthData, CancellationToken cancellationToken = default);
}