using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Industrial.Adam.AdminDashboard.Application.DTOs;

namespace Industrial.Adam.AdminDashboard.Infrastructure.SignalR;

/// <summary>
/// SignalR hub for real-time health updates
/// </summary>
[Authorize]
public class HealthUpdateHub : Hub
{
    private readonly ILogger<HealthUpdateHub> _logger;

    /// <summary>
    /// Constructor for HealthUpdateHub
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public HealthUpdateHub(ILogger<HealthUpdateHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects
    /// </summary>
    /// <returns>Task</returns>
    public override async Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;
        var user = Context.User?.Identity?.Name ?? "Anonymous";
        
        _logger.LogInformation("Health monitoring client connected: {ConnectionId} for user {User}", connectionId, user);
        
        // Add to health monitoring group
        await Groups.AddToGroupAsync(connectionId, "HealthMonitoring");
        
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects
    /// </summary>
    /// <param name="exception">Exception if any</param>
    /// <returns>Task</returns>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        var user = Context.User?.Identity?.Name ?? "Anonymous";
        
        _logger.LogInformation("Health monitoring client disconnected: {ConnectionId} for user {User}", connectionId, user);
        
        if (exception != null)
        {
            _logger.LogWarning(exception, "Client disconnected with exception: {ConnectionId}", connectionId);
        }
        
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe to specific service health updates
    /// </summary>
    /// <param name="serviceName">Service name to subscribe to</param>
    /// <returns>Task</returns>
    public async Task SubscribeToService(string serviceName)
    {
        var connectionId = Context.ConnectionId;
        var groupName = $"Service_{serviceName}";
        
        await Groups.AddToGroupAsync(connectionId, groupName);
        
        _logger.LogDebug("Client {ConnectionId} subscribed to service {ServiceName} updates", connectionId, serviceName);
    }

    /// <summary>
    /// Unsubscribe from specific service health updates
    /// </summary>
    /// <param name="serviceName">Service name to unsubscribe from</param>
    /// <returns>Task</returns>
    public async Task UnsubscribeFromService(string serviceName)
    {
        var connectionId = Context.ConnectionId;
        var groupName = $"Service_{serviceName}";
        
        await Groups.RemoveFromGroupAsync(connectionId, groupName);
        
        _logger.LogDebug("Client {ConnectionId} unsubscribed from service {ServiceName} updates", connectionId, serviceName);
    }
}

