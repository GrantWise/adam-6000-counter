using Microsoft.AspNetCore.SignalR;

namespace Industrial.Adam.Logger.WebApi.Hubs;

/// <summary>
/// SignalR hub for real-time health status updates
/// </summary>
public class HealthStatusHub : Hub
{
    private readonly ILogger<HealthStatusHub> _logger;

    public HealthStatusHub(ILogger<HealthStatusHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Health client connected: {ConnectionId}", Context.ConnectionId);
        
        // Automatically add to health updates group
        await Groups.AddToGroupAsync(Context.ConnectionId, "health-monitoring");
        
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Health client disconnected: {ConnectionId}", Context.ConnectionId);
        if (exception != null)
        {
            _logger.LogError(exception, "Health client disconnected with error");
        }
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe to health updates
    /// </summary>
    public async Task SubscribeToHealth()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "health-monitoring");
        _logger.LogInformation("Client {ConnectionId} subscribed to health updates", Context.ConnectionId);
    }

    /// <summary>
    /// Subscribe to system alerts only
    /// </summary>
    public async Task SubscribeToAlerts()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "alerts-only");
        _logger.LogInformation("Client {ConnectionId} subscribed to alerts", Context.ConnectionId);
    }

    /// <summary>
    /// Unsubscribe from health updates
    /// </summary>
    public async Task UnsubscribeFromHealth()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "health-monitoring");
        _logger.LogInformation("Client {ConnectionId} unsubscribed from health updates", Context.ConnectionId);
    }
}