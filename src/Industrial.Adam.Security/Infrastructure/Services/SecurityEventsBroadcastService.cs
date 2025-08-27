using Industrial.Adam.Security.Application.DTOs;
using Industrial.Adam.Security.Application.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Security.Infrastructure.Services;

/// <summary>
/// SignalR implementation of security events broadcasting service
/// </summary>
public class SecurityEventsBroadcastService : ISecurityEventsBroadcastService
{
    private readonly IHubContext<Hub> _hubContext;
    private readonly ILogger<SecurityEventsBroadcastService> _logger;

    public SecurityEventsBroadcastService(
        IHubContext<Hub> hubContext,
        ILogger<SecurityEventsBroadcastService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Broadcasts a security alert to all admin users
    /// </summary>
    public async Task BroadcastSecurityAlert(SecurityAlert alert)
    {
        try
        {
            await _hubContext.Clients.Group("AdminGroup").SendAsync("SecurityAlert", alert);
            _logger.LogInformation("Broadcasted security alert: {AlertType} - {Title}", alert.Type, alert.Title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast security alert");
        }
    }

    /// <summary>
    /// Broadcasts a login event to subscribed users
    /// </summary>
    public async Task BroadcastLoginEvent(LoginEvent loginEvent)
    {
        try
        {
            await _hubContext.Clients.Group("Events_Login").SendAsync("LoginEvent", loginEvent);
            _logger.LogDebug("Broadcasted login event for user: {Username}", loginEvent.Username);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast login event");
        }
    }

    /// <summary>
    /// Broadcasts an audit event to subscribed users
    /// </summary>
    public async Task BroadcastAuditEvent(AuditEvent auditEvent)
    {
        try
        {
            await _hubContext.Clients.Group("Events_Audit").SendAsync("AuditEvent", auditEvent);
            _logger.LogDebug("Broadcasted audit event: {Action} by user {UserId}", auditEvent.Action, auditEvent.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast audit event");
        }
    }

    /// <summary>
    /// Broadcasts user activity to subscribed users
    /// </summary>
    public async Task BroadcastUserActivity(UserActivityEvent activityEvent)
    {
        try
        {
            await _hubContext.Clients.Group("Events_Activity").SendAsync("UserActivityEvent", activityEvent);
            _logger.LogDebug("Broadcasted user activity: {Action} by user {UserId}", activityEvent.Action, activityEvent.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast user activity");
        }
    }

    /// <summary>
    /// Broadcasts system configuration changes
    /// </summary>
    public async Task BroadcastConfigurationChange(ConfigurationChangeEvent changeEvent)
    {
        try
        {
            await _hubContext.Clients.Group("Events_Config").SendAsync("ConfigurationChange", changeEvent);
            _logger.LogInformation("Broadcasted configuration change: {Key} by user {UserId}", changeEvent.Key, changeEvent.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast configuration change");
        }
    }

    /// <summary>
    /// Broadcasts session events
    /// </summary>
    public async Task BroadcastSessionEvent(SessionEvent sessionEvent)
    {
        try
        {
            await _hubContext.Clients.Group("Events_Session").SendAsync("SessionEvent", sessionEvent);
            _logger.LogDebug("Broadcasted session event: {EventType} for user {UserId}", sessionEvent.EventType, sessionEvent.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast session event");
        }
    }
}