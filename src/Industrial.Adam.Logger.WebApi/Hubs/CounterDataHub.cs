using Microsoft.AspNetCore.SignalR;

namespace Industrial.Adam.Logger.WebApi.Hubs;

/// <summary>
/// SignalR hub for real-time counter data updates
/// </summary>
public class CounterDataHub : Hub
{
    private readonly ILogger<CounterDataHub> _logger;

    public CounterDataHub(ILogger<CounterDataHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        if (exception != null)
        {
            _logger.LogError(exception, "Client disconnected with error");
        }
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe to updates from a specific device
    /// </summary>
    /// <param name="deviceId">Device ID to subscribe to</param>
    public async Task SubscribeToDevice(string deviceId)
    {
        if (string.IsNullOrEmpty(deviceId))
        {
            throw new ArgumentException("Device ID cannot be empty");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"device-{deviceId}");
        _logger.LogInformation("Client {ConnectionId} subscribed to device {DeviceId}", 
            Context.ConnectionId, deviceId);
    }

    /// <summary>
    /// Unsubscribe from updates from a specific device
    /// </summary>
    /// <param name="deviceId">Device ID to unsubscribe from</param>
    public async Task UnsubscribeFromDevice(string deviceId)
    {
        if (string.IsNullOrEmpty(deviceId))
        {
            throw new ArgumentException("Device ID cannot be empty");
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"device-{deviceId}");
        _logger.LogInformation("Client {ConnectionId} unsubscribed from device {DeviceId}", 
            Context.ConnectionId, deviceId);
    }

    /// <summary>
    /// Subscribe to updates from all devices
    /// </summary>
    public async Task SubscribeToAll()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "all-devices");
        _logger.LogInformation("Client {ConnectionId} subscribed to all devices", Context.ConnectionId);
    }

    /// <summary>
    /// Unsubscribe from all device updates
    /// </summary>
    public async Task UnsubscribeFromAll()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "all-devices");
        _logger.LogInformation("Client {ConnectionId} unsubscribed from all devices", Context.ConnectionId);
    }
}