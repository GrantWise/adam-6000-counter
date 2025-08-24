using Industrial.Adam.AdminDashboard.Domain.Entities;
using Industrial.Adam.AdminDashboard.Domain.Enums;

namespace Industrial.Adam.AdminDashboard.Domain.Interfaces;

/// <summary>
/// Repository interface for system alerts
/// </summary>
public interface ISystemAlertRepository
{
    /// <summary>
    /// Add a new alert
    /// </summary>
    /// <param name="alert">The alert to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The added alert</returns>
    Task<SystemAlert> AddAsync(SystemAlert alert, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get an alert by ID
    /// </summary>
    /// <param name="id">Alert ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The alert if found, null otherwise</returns>
    Task<SystemAlert?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active (unacknowledged) alerts
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of active alerts</returns>
    Task<IEnumerable<SystemAlert>> GetActiveAlertsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get alerts by severity level
    /// </summary>
    /// <param name="severity">Severity level to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of alerts with the specified severity</returns>
    Task<IEnumerable<SystemAlert>> GetAlertsBySeverityAsync(
        AlertSeverity severity, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Acknowledge an alert
    /// </summary>
    /// <param name="alertId">Alert ID</param>
    /// <param name="acknowledgedBy">User ID who acknowledged the alert</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successfully acknowledged, false otherwise</returns>
    Task<bool> AcknowledgeAlertAsync(
        int alertId, 
        string acknowledgedBy, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get alerts within a time range
    /// </summary>
    /// <param name="fromTime">Start time range</param>
    /// <param name="toTime">End time range</param>
    /// <param name="acknowledged">Filter by acknowledgment status (null for all)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of alerts within the time range</returns>
    Task<IEnumerable<SystemAlert>> GetAlertsAsync(
        DateTime fromTime, 
        DateTime toTime, 
        bool? acknowledged = null, 
        CancellationToken cancellationToken = default);
}