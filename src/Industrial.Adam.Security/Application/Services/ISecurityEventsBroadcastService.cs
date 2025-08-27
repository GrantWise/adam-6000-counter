using Industrial.Adam.Security.Application.DTOs;

namespace Industrial.Adam.Security.Application.Services;

/// <summary>
/// Interface for broadcasting security events through real-time communication
/// </summary>
public interface ISecurityEventsBroadcastService
{
    /// <summary>
    /// Broadcasts a security alert to all admin users
    /// </summary>
    Task BroadcastSecurityAlert(SecurityAlert alert);

    /// <summary>
    /// Broadcasts a login event to subscribed users
    /// </summary>
    Task BroadcastLoginEvent(LoginEvent loginEvent);

    /// <summary>
    /// Broadcasts an audit event to subscribed users
    /// </summary>
    Task BroadcastAuditEvent(AuditEvent auditEvent);

    /// <summary>
    /// Broadcasts user activity to subscribed users
    /// </summary>
    Task BroadcastUserActivity(UserActivityEvent activityEvent);

    /// <summary>
    /// Broadcasts system configuration changes
    /// </summary>
    Task BroadcastConfigurationChange(ConfigurationChangeEvent changeEvent);

    /// <summary>
    /// Broadcasts session events
    /// </summary>
    Task BroadcastSessionEvent(SessionEvent sessionEvent);
}