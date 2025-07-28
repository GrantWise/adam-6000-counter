// Industrial.Adam.Logger - WebSocket Health Monitoring Hub
// Real-time metrics streaming for live monitoring dashboards

using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Industrial.Adam.Logger.Configuration;
using Industrial.Adam.Logger.Monitoring;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Industrial.Adam.Logger.Monitoring;

/// <summary>
/// WebSocket hub for streaming real-time metrics to connected clients
/// Provides live updates for monitoring dashboards and alerting systems
/// </summary>
public class WebSocketHealthHub : ILiveMetricsStreamer, IHostedService, IDisposable
{
    private readonly ICounterMetricsCollector _metricsCollector;
    private readonly ILogger<WebSocketHealthHub> _logger;
    private readonly AdamLoggerConfig _config;

    // WebSocket connection management
    private readonly ConcurrentDictionary<string, WebSocketConnection> _connections = new();
    private readonly ConcurrentDictionary<string, MetricSubscription> _subscriptions = new();

    // Streaming control
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private Timer? _streamingTimer;
    private TimeSpan _updateInterval = TimeSpan.FromSeconds(5);
    private bool _isStreaming;

    // JSON serialization options for consistent formatting
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Initialize the WebSocket health hub
    /// </summary>
    /// <param name="metricsCollector">Metrics collector for data source</param>
    /// <param name="config">Logger configuration</param>
    /// <param name="logger">Logger for diagnostic information</param>
    public WebSocketHealthHub(
        ICounterMetricsCollector metricsCollector,
        IOptions<AdamLoggerConfig> config,
        ILogger<WebSocketHealthHub> logger)
    {
        _metricsCollector = metricsCollector ?? throw new ArgumentNullException(nameof(metricsCollector));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Start streaming metrics to connected clients
    /// </summary>
    /// <param name="updateInterval">Interval between metric updates</param>
    /// <returns>Task that completes when streaming is started</returns>
    public async Task StartStreamingAsync(TimeSpan updateInterval)
    {
        if (_isStreaming)
        {
            _logger.LogWarning("Metrics streaming is already active");
            return;
        }

        _updateInterval = updateInterval;

        _streamingTimer = new Timer(StreamMetricsToClients, null, TimeSpan.Zero, _updateInterval);
        _isStreaming = true;

        _logger.LogInformation("Started metrics streaming with {UpdateInterval}s interval",
            _updateInterval.TotalSeconds);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Stop streaming metrics
    /// </summary>
    /// <returns>Task that completes when streaming is stopped</returns>
    public async Task StopStreamingAsync()
    {
        if (!_isStreaming)
        {
            return;
        }

        _streamingTimer?.Dispose();
        _streamingTimer = null;
        _isStreaming = false;

        // Notify all connected clients about streaming stop
        var stopMessage = JsonSerializer.Serialize(new
        {
            Type = "StreamingStopped",
            Timestamp = DateTimeOffset.UtcNow,
            Message = "Metrics streaming has been stopped"
        }, _jsonOptions);

        await BroadcastToAllClientsAsync(stopMessage);

        _logger.LogInformation("Stopped metrics streaming");
    }

    /// <summary>
    /// Subscribe to real-time metrics updates
    /// </summary>
    /// <param name="callback">Callback function to receive metric updates</param>
    /// <returns>Subscription ID for managing the subscription</returns>
    public string Subscribe(Func<MetricsSnapshot, Task> callback)
    {
        var subscriptionId = Guid.NewGuid().ToString();
        var subscription = new MetricSubscription
        {
            Id = subscriptionId,
            Callback = callback,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _subscriptions[subscriptionId] = subscription;

        _logger.LogDebug("Created metrics subscription {SubscriptionId}", subscriptionId);
        return subscriptionId;
    }

    /// <summary>
    /// Unsubscribe from metrics updates
    /// </summary>
    /// <param name="subscriptionId">Subscription ID to remove</param>
    /// <returns>True if subscription was found and removed</returns>
    public bool Unsubscribe(string subscriptionId)
    {
        var removed = _subscriptions.TryRemove(subscriptionId, out var subscription);
        if (removed)
        {
            _logger.LogDebug("Removed metrics subscription {SubscriptionId}", subscriptionId);
        }
        else
        {
            _logger.LogWarning("Attempted to remove non-existent subscription {SubscriptionId}", subscriptionId);
        }
        return removed;
    }

    /// <summary>
    /// Get number of active subscribers
    /// </summary>
    /// <returns>Number of active metric subscriptions</returns>
    public int GetActiveSubscriberCount()
    {
        return _subscriptions.Count + _connections.Count;
    }

    /// <summary>
    /// Add a WebSocket connection for metrics streaming
    /// </summary>
    /// <param name="webSocket">WebSocket connection</param>
    /// <param name="clientId">Optional client identifier</param>
    /// <returns>Connection ID for managing the connection</returns>
    public async Task<string> AddWebSocketConnectionAsync(WebSocket webSocket, string? clientId = null)
    {
        var connectionId = clientId ?? Guid.NewGuid().ToString();
        var connection = new WebSocketConnection
        {
            Id = connectionId,
            WebSocket = webSocket,
            ConnectedAt = DateTimeOffset.UtcNow
        };

        _connections[connectionId] = connection;

        _logger.LogInformation("Added WebSocket connection {ConnectionId}", connectionId);

        // Send welcome message with current metrics
        try
        {
            var currentMetrics = await _metricsCollector.GetCounterMetricsAsync();
            var welcomeMessage = JsonSerializer.Serialize(new
            {
                Type = "Welcome",
                ConnectionId = connectionId,
                Timestamp = DateTimeOffset.UtcNow,
                CurrentMetrics = currentMetrics
            }, _jsonOptions);

            await SendToWebSocketAsync(webSocket, welcomeMessage);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error sending welcome message to connection {ConnectionId}", connectionId);
        }

        return connectionId;
    }

    /// <summary>
    /// Remove a WebSocket connection
    /// </summary>
    /// <param name="connectionId">Connection ID to remove</param>
    /// <returns>True if connection was found and removed</returns>
    public bool RemoveWebSocketConnection(string connectionId)
    {
        var removed = _connections.TryRemove(connectionId, out var connection);
        if (removed)
        {
            try
            {
                connection?.WebSocket?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing WebSocket for connection {ConnectionId}", connectionId);
            }

            _logger.LogInformation("Removed WebSocket connection {ConnectionId}", connectionId);
        }
        return removed;
    }

    /// <summary>
    /// Stream metrics to all connected clients
    /// </summary>
    private async void StreamMetricsToClients(object? state)
    {
        if (!_isStreaming)
            return;

        try
        {
            var metrics = await _metricsCollector.GetCounterMetricsAsync();

            // Notify callback subscriptions
            var subscriptionTasks = _subscriptions.Values.Select(async sub =>
            {
                try
                {
                    await sub.Callback(metrics);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error in metrics subscription callback {SubscriptionId}", sub.Id);
                }
            });

            await Task.WhenAll(subscriptionTasks);

            // Send to WebSocket connections
            var metricsMessage = JsonSerializer.Serialize(new
            {
                Type = "MetricsUpdate",
                Timestamp = DateTimeOffset.UtcNow,
                Metrics = metrics
            }, _jsonOptions);

            await BroadcastToAllClientsAsync(metricsMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming metrics to clients");
        }
    }

    /// <summary>
    /// Broadcast message to all connected WebSocket clients
    /// </summary>
    private async Task BroadcastToAllClientsAsync(string message)
    {
        var disconnectedClients = new List<string>();

        var broadcastTasks = _connections.Select(async kvp =>
        {
            try
            {
                await SendToWebSocketAsync(kvp.Value.WebSocket, message);
            }
            catch (WebSocketException)
            {
                // Client disconnected
                disconnectedClients.Add(kvp.Key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error sending message to WebSocket client {ConnectionId}", kvp.Key);
                disconnectedClients.Add(kvp.Key);
            }
        });

        await Task.WhenAll(broadcastTasks);

        // Clean up disconnected clients
        foreach (var disconnectedId in disconnectedClients)
        {
            RemoveWebSocketConnection(disconnectedId);
        }
    }

    /// <summary>
    /// Send message to a specific WebSocket connection
    /// </summary>
    private async Task SendToWebSocketAsync(WebSocket webSocket, string message)
    {
        if (webSocket.State != WebSocketState.Open)
        {
            throw new WebSocketException("WebSocket is not in Open state");
        }

        var messageBytes = Encoding.UTF8.GetBytes(message);
        var buffer = new ArraySegment<byte>(messageBytes);

        await webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, _cancellationTokenSource.Token);
    }

    /// <summary>
    /// Handle WebSocket message from client
    /// </summary>
    /// <param name="connectionId">Connection ID</param>
    /// <param name="message">Message received from client</param>
    public async Task HandleClientMessageAsync(string connectionId, string message)
    {
        try
        {
            using var jsonDoc = JsonDocument.Parse(message);
            var root = jsonDoc.RootElement;

            if (root.TryGetProperty("type", out var typeElement))
            {
                var messageType = typeElement.GetString();

                switch (messageType?.ToLowerInvariant())
                {
                    case "ping":
                        await SendPongResponseAsync(connectionId);
                        break;

                    case "get_current_metrics":
                        await SendCurrentMetricsAsync(connectionId);
                        break;

                    case "change_update_interval":
                        if (root.TryGetProperty("intervalSeconds", out var intervalElement))
                        {
                            var newInterval = TimeSpan.FromSeconds(intervalElement.GetDouble());
                            await ChangeUpdateIntervalAsync(newInterval);
                        }
                        break;

                    default:
                        _logger.LogWarning("Unknown message type '{MessageType}' from connection {ConnectionId}",
                            messageType, connectionId);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling client message from connection {ConnectionId}: {Message}",
                connectionId, message);
        }
    }

    /// <summary>
    /// Send pong response to client ping
    /// </summary>
    private async Task SendPongResponseAsync(string connectionId)
    {
        if (_connections.TryGetValue(connectionId, out var connection))
        {
            var pongMessage = JsonSerializer.Serialize(new
            {
                Type = "Pong",
                Timestamp = DateTimeOffset.UtcNow
            }, _jsonOptions);

            await SendToWebSocketAsync(connection.WebSocket, pongMessage);
        }
    }

    /// <summary>
    /// Send current metrics to specific client
    /// </summary>
    private async Task SendCurrentMetricsAsync(string connectionId)
    {
        if (_connections.TryGetValue(connectionId, out var connection))
        {
            var metrics = await _metricsCollector.GetCurrentMetricsAsync();
            var metricsMessage = JsonSerializer.Serialize(new
            {
                Type = "CurrentMetrics",
                Timestamp = DateTimeOffset.UtcNow,
                Metrics = metrics
            }, _jsonOptions);

            await SendToWebSocketAsync(connection.WebSocket, metricsMessage);
        }
    }

    /// <summary>
    /// Change the update interval for metrics streaming
    /// </summary>
    private async Task ChangeUpdateIntervalAsync(TimeSpan newInterval)
    {
        if (newInterval.TotalSeconds < 1 || newInterval.TotalSeconds > 300)
        {
            _logger.LogWarning("Invalid update interval {Seconds}s - must be between 1 and 300 seconds",
                newInterval.TotalSeconds);
            return;
        }

        _updateInterval = newInterval;

        if (_isStreaming)
        {
            await StopStreamingAsync();
            await StartStreamingAsync(_updateInterval);
        }

        _logger.LogInformation("Changed metrics update interval to {Seconds}s", newInterval.TotalSeconds);
    }

    #region IHostedService Implementation

    /// <summary>
    /// Start the hosted service
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting WebSocket health hub");

        // Start streaming with default interval
        await StartStreamingAsync(_updateInterval);
    }

    /// <summary>
    /// Stop the hosted service
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping WebSocket health hub");

        // Stop streaming
        await StopStreamingAsync();

        // Close all WebSocket connections
        var closeTasks = _connections.Values.Select(async connection =>
        {
            try
            {
                if (connection.WebSocket.State == WebSocketState.Open)
                {
                    await connection.WebSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Service shutting down",
                        CancellationToken.None);
                }
                connection.WebSocket.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error closing WebSocket connection {ConnectionId}", connection.Id);
            }
        });

        await Task.WhenAll(closeTasks);

        _connections.Clear();
        _subscriptions.Clear();
    }

    #endregion

    /// <summary>
    /// Dispose of resources
    /// </summary>
    public void Dispose()
    {
        try
        {
            _cancellationTokenSource?.Cancel();
            _streamingTimer?.Dispose();
            _cancellationTokenSource?.Dispose();

            // Dispose all WebSocket connections
            foreach (var connection in _connections.Values)
            {
                try
                {
                    connection.WebSocket?.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing WebSocket connection {ConnectionId}", connection.Id);
                }
            }

            _connections.Clear();
            _subscriptions.Clear();

            _logger.LogInformation("WebSocket health hub disposed");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during WebSocket health hub disposal");
        }
    }
}

/// <summary>
/// WebSocket connection tracking
/// </summary>
internal class WebSocketConnection
{
    public string Id { get; set; } = string.Empty;
    public WebSocket WebSocket { get; set; } = null!;
    public DateTimeOffset ConnectedAt { get; set; }
}

/// <summary>
/// Metrics subscription tracking
/// </summary>
internal class MetricSubscription
{
    public string Id { get; set; } = string.Empty;
    public Func<MetricsSnapshot, Task> Callback { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
}
