using Industrial.Adam.Security.Application.DTOs;
using Industrial.Adam.Security.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Industrial.Adam.Security.Hubs;

/// <summary>
/// SignalR hub for real-time security events and notifications
/// </summary>
[Authorize(Policy = "RequireAdmin")]
public class SecurityEventsHub : Hub
{
    private readonly ILogger<SecurityEventsHub> _logger;
    
    public SecurityEventsHub(ILogger<SecurityEventsHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects to the hub
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var username = Context.User?.Identity?.Name;
        var connectionId = Context.ConnectionId;

        _logger.LogInformation("Security events connection established: User {Username} ({UserId}), Connection {ConnectionId}", 
            username, userId, connectionId);

        // Add to admin group for broadcasting
        await Groups.AddToGroupAsync(connectionId, "AdminGroup");

        // Send initial connection success message
        await Clients.Caller.SendAsync("ConnectionEstablished", new
        {
            Message = "Connected to security events hub",
            UserId = userId,
            Username = username,
            ConnectedAt = DateTimeOffset.UtcNow
        });

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var username = Context.User?.Identity?.Name;
        var connectionId = Context.ConnectionId;

        _logger.LogInformation("Security events connection closed: User {Username} ({UserId}), Connection {ConnectionId}", 
            username, userId, connectionId);

        if (exception != null)
        {
            _logger.LogError(exception, "Security events connection closed with error for user {Username}", username);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe to specific security event types
    /// </summary>
    /// <param name="eventTypes">Event types to subscribe to</param>
    public async Task SubscribeToEvents(string[] eventTypes)
    {
        var connectionId = Context.ConnectionId;
        var username = Context.User?.Identity?.Name;

        foreach (var eventType in eventTypes)
        {
            await Groups.AddToGroupAsync(connectionId, $"Events_{eventType}");
            _logger.LogDebug("User {Username} subscribed to event type: {EventType}", username, eventType);
        }

        await Clients.Caller.SendAsync("SubscriptionConfirmed", new
        {
            EventTypes = eventTypes,
            Message = $"Subscribed to {eventTypes.Length} event types"
        });
    }

    /// <summary>
    /// Unsubscribe from specific security event types
    /// </summary>
    /// <param name="eventTypes">Event types to unsubscribe from</param>
    public async Task UnsubscribeFromEvents(string[] eventTypes)
    {
        var connectionId = Context.ConnectionId;
        var username = Context.User?.Identity?.Name;

        foreach (var eventType in eventTypes)
        {
            await Groups.RemoveFromGroupAsync(connectionId, $"Events_{eventType}");
            _logger.LogDebug("User {Username} unsubscribed from event type: {EventType}", username, eventType);
        }

        await Clients.Caller.SendAsync("UnsubscriptionConfirmed", new
        {
            EventTypes = eventTypes,
            Message = $"Unsubscribed from {eventTypes.Length} event types"
        });
    }

    /// <summary>
    /// Get current connection status
    /// </summary>
    public async Task GetConnectionStatus()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var username = Context.User?.Identity?.Name;
        var connectionId = Context.ConnectionId;

        await Clients.Caller.SendAsync("ConnectionStatus", new
        {
            ConnectionId = connectionId,
            UserId = userId,
            Username = username,
            IsConnected = true,
            ConnectedSince = DateTimeOffset.UtcNow // This would be tracked properly in production
        });
    }
}