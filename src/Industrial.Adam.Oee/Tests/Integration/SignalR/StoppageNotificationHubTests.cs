using FluentAssertions;
using Industrial.Adam.Oee.Application.Events;
using Industrial.Adam.Oee.Domain.Events;
using Industrial.Adam.Oee.Infrastructure.SignalR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Industrial.Adam.Oee.Tests.Integration.SignalR;

/// <summary>
/// Integration tests for StoppageNotificationHub
/// </summary>
public class StoppageNotificationHubTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _hubUrl;
    private HubConnection? _connection;

    public StoppageNotificationHubTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _hubUrl = "/stoppageHub";
    }

    [Fact]
    public async Task Hub_CanEstablishConnection_Successfully()
    {
        // Arrange
        var connection = await CreateConnectionAsync();

        // Act & Assert
        connection.State.Should().Be(HubConnectionState.Connected);
    }

    [Fact]
    public async Task Hub_CanSubscribeToLine_Successfully()
    {
        // Arrange
        var connection = await CreateConnectionAsync();
        var subscriptionConfirmed = false;
        var subscriptionInfo = new SubscriptionInfo("", "", "");

        connection.On<SubscriptionInfo>("SubscriptionConfirmed", (info) =>
        {
            subscriptionConfirmed = true;
            subscriptionInfo = info;
        });

        // Act
        await connection.InvokeAsync("SubscribeToLine", "LINE001");

        // Wait for confirmation
        await WaitForConditionAsync(() => subscriptionConfirmed, TimeSpan.FromSeconds(5));

        // Assert
        subscriptionConfirmed.Should().BeTrue();
        subscriptionInfo.LineId.Should().Be("LINE001");
        subscriptionInfo.Message.Should().Contain("subscription confirmed");
    }

    [Fact]
    public async Task Hub_CanSubscribeAsOperator_Successfully()
    {
        // Arrange
        var connection = await CreateConnectionAsync();
        var subscriptionConfirmed = false;
        var operatorInfo = new OperatorSubscriptionInfo("", Array.Empty<string>(), "");

        connection.On<OperatorSubscriptionInfo>("OperatorSubscriptionConfirmed", (info) =>
        {
            subscriptionConfirmed = true;
            operatorInfo = info;
        });

        // Act
        var lineIds = new[] { "LINE001", "LINE002" };
        await connection.InvokeAsync("SubscribeAsOperator", lineIds, "OP001");

        // Wait for confirmation
        await WaitForConditionAsync(() => subscriptionConfirmed, TimeSpan.FromSeconds(5));

        // Assert
        subscriptionConfirmed.Should().BeTrue();
        operatorInfo.OperatorId.Should().Be("OP001");
        operatorInfo.LineIds.Should().BeEquivalentTo(lineIds);
    }

    [Fact]
    public async Task Hub_CanSubscribeAsSupervisor_Successfully()
    {
        // Arrange
        var connection = await CreateConnectionAsync();
        var subscriptionConfirmed = false;
        var supervisorInfo = new SupervisorSubscriptionInfo("", "", "");

        connection.On<SupervisorSubscriptionInfo>("SupervisorSubscriptionConfirmed", (info) =>
        {
            subscriptionConfirmed = true;
            supervisorInfo = info;
        });

        // Act
        await connection.InvokeAsync("SubscribeAsSupervisor", "SUP001");

        // Wait for confirmation
        await WaitForConditionAsync(() => subscriptionConfirmed, TimeSpan.FromSeconds(5));

        // Assert
        subscriptionConfirmed.Should().BeTrue();
        supervisorInfo.SupervisorId.Should().Be("SUP001");
        supervisorInfo.GroupName.Should().Be("Supervisors");
    }

    [Fact]
    public async Task Hub_CanUnsubscribeFromLine_Successfully()
    {
        // Arrange
        var connection = await CreateConnectionAsync();
        var subscriptionRemoved = false;
        var subscriptionInfo = new SubscriptionInfo("", "", "");

        // First subscribe
        await connection.InvokeAsync("SubscribeToLine", "LINE001");

        // Setup unsubscribe handler
        connection.On<SubscriptionInfo>("SubscriptionRemoved", (info) =>
        {
            subscriptionRemoved = true;
            subscriptionInfo = info;
        });

        // Act
        await connection.InvokeAsync("UnsubscribeFromLine", "LINE001");

        // Wait for confirmation
        await WaitForConditionAsync(() => subscriptionRemoved, TimeSpan.FromSeconds(5));

        // Assert
        subscriptionRemoved.Should().BeTrue();
        subscriptionInfo.LineId.Should().Be("LINE001");
        subscriptionInfo.Message.Should().Contain("subscription removed");
    }

    [Fact]
    public async Task Hub_RespondsToHeartbeat_Successfully()
    {
        // Arrange
        var connection = await CreateConnectionAsync();
        var heartbeatReceived = false;
        var heartbeatInfo = new HeartbeatInfo("", DateTime.MinValue, "");

        connection.On<HeartbeatInfo>("HeartbeatResponse", (info) =>
        {
            heartbeatReceived = true;
            heartbeatInfo = info;
        });

        // Act
        await connection.InvokeAsync("Heartbeat");

        // Wait for response
        await WaitForConditionAsync(() => heartbeatReceived, TimeSpan.FromSeconds(5));

        // Assert
        heartbeatReceived.Should().BeTrue();
        heartbeatInfo.ConnectionId.Should().NotBeEmpty();
        heartbeatInfo.Status.Should().Be("Healthy");
        heartbeatInfo.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Hub_HandleInvalidSubscription_ReturnsError()
    {
        // Arrange
        var connection = await CreateConnectionAsync();
        var errorReceived = false;
        var errorMessage = "";

        connection.On<string>("SubscriptionError", (error) =>
        {
            errorReceived = true;
            errorMessage = error;
        });

        // Act - Try to subscribe with empty line ID
        await connection.InvokeAsync("SubscribeToLine", "");

        // Wait for error
        await WaitForConditionAsync(() => errorReceived, TimeSpan.FromSeconds(5));

        // Assert
        errorReceived.Should().BeTrue();
        errorMessage.Should().Contain("cannot be empty");
    }

    [Fact]
    public async Task Hub_CanReceiveStoppageNotification_WhenSubscribed()
    {
        // Arrange
        var connection = await CreateConnectionAsync();
        var notificationReceived = false;
        var receivedNotification = new StoppageNotificationData(0, "", "", DateTime.MinValue, DateTime.MinValue, 0, false, NotificationUrgency.Low, "");

        // Subscribe to line
        await connection.InvokeAsync("SubscribeToLine", "LINE001");

        // Setup notification handler
        connection.On<StoppageNotificationData>("StoppageDetected", (notification) =>
        {
            notificationReceived = true;
            receivedNotification = notification;
        });

        // Act
        // Simulate sending notification through the hub context
        var notificationService = _factory.Services.GetRequiredService<IStoppageNotificationService>();
        var testNotification = new StoppageNotificationData(
            1,
            "LINE001",
            "WO123",
            DateTime.UtcNow.AddMinutes(-10),
            DateTime.UtcNow,
            10.0,
            true,
            NotificationUrgency.Medium,
            "Test stoppage detected"
        );

        await notificationService.NotifyLineOperatorsAsync("LINE001", testNotification);

        // Wait for notification
        await WaitForConditionAsync(() => notificationReceived, TimeSpan.FromSeconds(10));

        // Assert
        notificationReceived.Should().BeTrue();
        receivedNotification.StoppageId.Should().Be(testNotification.StoppageId);
        receivedNotification.LineId.Should().Be(testNotification.LineId);
        receivedNotification.Summary.Should().Be(testNotification.Summary);
    }

    /// <summary>
    /// Create and start a SignalR connection to the hub
    /// </summary>
    private async Task<HubConnection> CreateConnectionAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync();
        }

        var client = _factory.CreateClient();
        _connection = new HubConnectionBuilder()
            .WithUrl($"{client.BaseAddress!.ToString().TrimEnd('/')}{_hubUrl}", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        await _connection.StartAsync();
        return _connection;
    }

    /// <summary>
    /// Wait for a condition to be met with timeout
    /// </summary>
    private static async Task WaitForConditionAsync(Func<bool> condition, TimeSpan timeout)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        while (!condition() && stopwatch.Elapsed < timeout)
        {
            await Task.Delay(100);
        }

        if (!condition())
        {
            throw new TimeoutException($"Condition was not met within {timeout}");
        }
    }

    /// <summary>
    /// Dispose resources
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }
}
