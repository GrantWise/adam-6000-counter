// Industrial.Adam.Logger.Tests - WebSocketHealthHub Unit Tests
// Comprehensive tests for WebSocket-based real-time metrics streaming

using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Industrial.Adam.Logger.Configuration;
using Industrial.Adam.Logger.Monitoring;
using Industrial.Adam.Logger.Performance;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Industrial.Adam.Logger.Tests.Monitoring;

public class WebSocketHealthHubTests : IDisposable
{
    private readonly Mock<ICounterMetricsCollector> _mockMetricsCollector;
    private readonly Mock<ILogger<WebSocketHealthHub>> _mockLogger;
    private readonly Mock<IOptions<AdamLoggerConfig>> _mockOptions;
    private readonly AdamLoggerConfig _config;
    private readonly WebSocketHealthHub _hub;

    public WebSocketHealthHubTests()
    {
        _mockMetricsCollector = new Mock<ICounterMetricsCollector>();
        _mockLogger = new Mock<ILogger<WebSocketHealthHub>>();
        _mockOptions = new Mock<IOptions<AdamLoggerConfig>>();

        _config = new AdamLoggerConfig
        {
            Devices = new List<AdamDeviceConfig>
            {
                new AdamDeviceConfig
                {
                    DeviceId = "test-device",
                    IpAddress = "192.168.1.100",
                    Port = 502,
                    UnitId = 1,
                    TimeoutMs = 5000,
                    Channels = new List<ChannelConfig>
                    {
                        new ChannelConfig { ChannelNumber = 0, Name = "Channel0", StartRegister = 0 }
                    }
                }
            }
        };

        _mockOptions.Setup(o => o.Value).Returns(_config);
        _hub = new WebSocketHealthHub(_mockMetricsCollector.Object, _mockOptions.Object, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_ValidParameters_ShouldCreateInstance()
    {
        // Arrange & Act
        var hub = new WebSocketHealthHub(_mockMetricsCollector.Object, _mockOptions.Object, _mockLogger.Object);

        // Assert
        hub.Should().NotBeNull();
        hub.GetActiveSubscriberCount().Should().Be(0);
    }

    [Fact]
    public void Constructor_NullMetricsCollector_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var action = () => new WebSocketHealthHub(null!, _mockOptions.Object, _mockLogger.Object);
        action.Should().Throw<ArgumentNullException>().WithParameterName("metricsCollector");
    }

    [Fact]
    public void Constructor_NullConfig_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var action = () => new WebSocketHealthHub(_mockMetricsCollector.Object, null!, _mockLogger.Object);
        action.Should().Throw<ArgumentNullException>().WithParameterName("config");
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var action = () => new WebSocketHealthHub(_mockMetricsCollector.Object, _mockOptions.Object, null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task StartStreamingAsync_ValidInterval_ShouldStartStreaming()
    {
        // Arrange
        var updateInterval = TimeSpan.FromSeconds(10);

        // Act
        await _hub.StartStreamingAsync(updateInterval);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Started metrics streaming")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartStreamingAsync_AlreadyStreaming_ShouldLogWarning()
    {
        // Arrange
        var updateInterval = TimeSpan.FromSeconds(10);
        await _hub.StartStreamingAsync(updateInterval);

        // Act
        await _hub.StartStreamingAsync(updateInterval);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Metrics streaming is already active")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StopStreamingAsync_WhenStreaming_ShouldStopStreaming()
    {
        // Arrange
        await _hub.StartStreamingAsync(TimeSpan.FromSeconds(10));

        // Act
        await _hub.StopStreamingAsync();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Stopped metrics streaming")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StopStreamingAsync_WhenNotStreaming_ShouldNotThrow()
    {
        // Act & Assert
        await _hub.StopStreamingAsync(); // Should not throw
    }

    [Fact]
    public void Subscribe_ValidCallback_ShouldReturnSubscriptionId()
    {
        // Arrange
        Func<MetricsSnapshot, Task> callback = _ => Task.CompletedTask;

        // Act
        var subscriptionId = _hub.Subscribe(callback);

        // Assert
        subscriptionId.Should().NotBeNullOrEmpty();
        _hub.GetActiveSubscriberCount().Should().Be(1);
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Created metrics subscription")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Subscribe_MultipleCallbacks_ShouldReturnDifferentSubscriptionIds()
    {
        // Arrange
        Func<MetricsSnapshot, Task> callback1 = _ => Task.CompletedTask;
        Func<MetricsSnapshot, Task> callback2 = _ => Task.CompletedTask;

        // Act
        var subscriptionId1 = _hub.Subscribe(callback1);
        var subscriptionId2 = _hub.Subscribe(callback2);

        // Assert
        subscriptionId1.Should().NotBe(subscriptionId2);
        _hub.GetActiveSubscriberCount().Should().Be(2);
    }

    [Fact]
    public void Unsubscribe_ExistingSubscription_ShouldReturnTrue()
    {
        // Arrange
        var subscriptionId = _hub.Subscribe(_ => Task.CompletedTask);

        // Act
        var result = _hub.Unsubscribe(subscriptionId);

        // Assert
        result.Should().BeTrue();
        _hub.GetActiveSubscriberCount().Should().Be(0);
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Removed metrics subscription")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Unsubscribe_NonExistentSubscription_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid().ToString();

        // Act
        var result = _hub.Unsubscribe(nonExistentId);

        // Assert
        result.Should().BeFalse();
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Attempted to remove non-existent subscription")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void GetActiveSubscriberCount_WithSubscriptionsAndConnections_ShouldReturnTotalCount()
    {
        // Arrange
        var subscription1 = _hub.Subscribe(_ => Task.CompletedTask);
        var subscription2 = _hub.Subscribe(_ => Task.CompletedTask);

        // Act
        var count = _hub.GetActiveSubscriberCount();

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public async Task AddWebSocketConnectionAsync_ValidWebSocket_ShouldReturnConnectionId()
    {
        // Arrange
        var mockWebSocket = new Mock<WebSocket>();
        mockWebSocket.Setup(ws => ws.State).Returns(WebSocketState.Open);
        
        var mockMetrics = new CounterMetricsSnapshot
        {
            Timestamp = DateTimeOffset.UtcNow,
            CounterChannels = new Dictionary<string, CounterChannelMetrics>()
        };
        
        _mockMetricsCollector.Setup(m => m.GetCounterMetricsAsync())
            .ReturnsAsync(mockMetrics);

        // Act
        var connectionId = await _hub.AddWebSocketConnectionAsync(mockWebSocket.Object);

        // Assert
        connectionId.Should().NotBeNullOrEmpty();
        _hub.GetActiveSubscriberCount().Should().Be(1);
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Added WebSocket connection")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task AddWebSocketConnectionAsync_WithCustomClientId_ShouldUseProvidedId()
    {
        // Arrange
        var mockWebSocket = new Mock<WebSocket>();
        mockWebSocket.Setup(ws => ws.State).Returns(WebSocketState.Open);
        var clientId = "custom-client-123";
        
        var mockMetrics = new CounterMetricsSnapshot
        {
            Timestamp = DateTimeOffset.UtcNow,
            CounterChannels = new Dictionary<string, CounterChannelMetrics>()
        };
        
        _mockMetricsCollector.Setup(m => m.GetCounterMetricsAsync())
            .ReturnsAsync(mockMetrics);

        // Act
        var connectionId = await _hub.AddWebSocketConnectionAsync(mockWebSocket.Object, clientId);

        // Assert
        connectionId.Should().Be(clientId);
    }

    [Fact]
    public async Task AddWebSocketConnectionAsync_MetricsCollectorThrows_ShouldLogWarning()
    {
        // Arrange
        var mockWebSocket = new Mock<WebSocket>();
        mockWebSocket.Setup(ws => ws.State).Returns(WebSocketState.Open);
        
        _mockMetricsCollector.Setup(m => m.GetCounterMetricsAsync())
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        // Act
        var connectionId = await _hub.AddWebSocketConnectionAsync(mockWebSocket.Object);

        // Assert
        connectionId.Should().NotBeNullOrEmpty();
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error sending welcome message")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RemoveWebSocketConnection_ExistingConnection_ShouldReturnTrue()
    {
        // Arrange
        var mockWebSocket = new Mock<WebSocket>();
        mockWebSocket.Setup(ws => ws.State).Returns(WebSocketState.Open);
        var connectionId = "test-connection";
        
        // Add connection first (simulating manual addition)
        await _hub.AddWebSocketConnectionAsync(mockWebSocket.Object, connectionId);

        // Act
        var result = _hub.RemoveWebSocketConnection(connectionId);

        // Assert
        result.Should().BeTrue();
        _hub.GetActiveSubscriberCount().Should().Be(0);
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Removed WebSocket connection")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RemoveWebSocketConnection_NonExistentConnection_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentId = "non-existent-connection";

        // Act
        var result = _hub.RemoveWebSocketConnection(nonExistentId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HandleClientMessageAsync_PingMessage_ShouldSendPongResponse()
    {
        // Arrange
        var mockWebSocket = new Mock<WebSocket>();
        mockWebSocket.Setup(ws => ws.State).Returns(WebSocketState.Open);
        
        var connectionId = await _hub.AddWebSocketConnectionAsync(mockWebSocket.Object);
        var pingMessage = JsonSerializer.Serialize(new { type = "ping" });

        // Act
        await _hub.HandleClientMessageAsync(connectionId, pingMessage);

        // Assert
        // Should attempt to send pong response
        mockWebSocket.Verify(ws => ws.SendAsync(
            It.IsAny<ArraySegment<byte>>(),
            WebSocketMessageType.Text,
            true,
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task HandleClientMessageAsync_GetCurrentMetricsMessage_ShouldSendMetrics()
    {
        // Arrange
        var mockWebSocket = new Mock<WebSocket>();
        mockWebSocket.Setup(ws => ws.State).Returns(WebSocketState.Open);
        
        var mockMetrics = new MetricsSnapshot
        {
            Timestamp = DateTimeOffset.UtcNow,
            Performance = new PerformanceMetrics { CpuUsagePercent = 25.0 }
        };
        
        _mockMetricsCollector.Setup(m => m.GetCurrentMetricsAsync())
            .ReturnsAsync(mockMetrics);

        var connectionId = await _hub.AddWebSocketConnectionAsync(mockWebSocket.Object);
        var requestMessage = JsonSerializer.Serialize(new { type = "get_current_metrics" });

        // Act
        await _hub.HandleClientMessageAsync(connectionId, requestMessage);

        // Assert
        // Should send metrics response
        mockWebSocket.Verify(ws => ws.SendAsync(
            It.IsAny<ArraySegment<byte>>(),
            WebSocketMessageType.Text,
            true,
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task HandleClientMessageAsync_ChangeUpdateIntervalMessage_ShouldChangeInterval()
    {
        // Arrange
        var mockWebSocket = new Mock<WebSocket>();
        mockWebSocket.Setup(ws => ws.State).Returns(WebSocketState.Open);
        
        var connectionId = await _hub.AddWebSocketConnectionAsync(mockWebSocket.Object);
        var changeIntervalMessage = JsonSerializer.Serialize(new 
        { 
            type = "change_update_interval",
            intervalSeconds = 30.0
        });

        // Act
        await _hub.HandleClientMessageAsync(connectionId, changeIntervalMessage);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Changed metrics update interval")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleClientMessageAsync_UnknownMessageType_ShouldLogWarning()
    {
        // Arrange
        var mockWebSocket = new Mock<WebSocket>();
        mockWebSocket.Setup(ws => ws.State).Returns(WebSocketState.Open);
        
        var connectionId = await _hub.AddWebSocketConnectionAsync(mockWebSocket.Object);
        var unknownMessage = JsonSerializer.Serialize(new { type = "unknown_message" });

        // Act
        await _hub.HandleClientMessageAsync(connectionId, unknownMessage);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unknown message type")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleClientMessageAsync_InvalidJsonMessage_ShouldLogError()
    {
        // Arrange
        var mockWebSocket = new Mock<WebSocket>();
        mockWebSocket.Setup(ws => ws.State).Returns(WebSocketState.Open);
        
        var connectionId = await _hub.AddWebSocketConnectionAsync(mockWebSocket.Object);
        var invalidMessage = "{ invalid json }";

        // Act
        await _hub.HandleClientMessageAsync(connectionId, invalidMessage);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error handling client message")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_ShouldStartStreaming()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;

        // Act
        await _hub.StartAsync(cancellationToken);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting WebSocket health hub")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StopAsync_ShouldStopStreamingAndCloseConnections()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        await _hub.StartAsync(cancellationToken);

        // Act
        await _hub.StopAsync(cancellationToken);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Stopping WebSocket health hub")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Dispose_ShouldCleanupResources()
    {
        // Arrange
        var subscription = _hub.Subscribe(_ => Task.CompletedTask);

        // Act
        _hub.Dispose();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("WebSocket health hub disposed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Dispose_WithException_ShouldLogWarning()
    {
        // Arrange
        var mockWebSocket = new Mock<WebSocket>();
        mockWebSocket.Setup(ws => ws.Dispose()).Throws(new InvalidOperationException("Test exception"));
        
        // Add connection that will throw during disposal
        await _hub.AddWebSocketConnectionAsync(mockWebSocket.Object);

        // Act
        _hub.Dispose();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error disposing WebSocket connection")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Dispose_MultipleDispose_ShouldNotThrow()
    {
        // Act & Assert
        _hub.Dispose(); // Should not throw
        _hub.Dispose(); // Should not throw (idempotent)
    }

    public void Dispose()
    {
        _hub?.Dispose();
    }
}