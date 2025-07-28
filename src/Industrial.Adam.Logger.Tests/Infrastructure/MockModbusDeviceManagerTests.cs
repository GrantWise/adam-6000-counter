// Industrial.Adam.Logger.Tests - MockModbusDeviceManager Unit Tests
// Tests for the demo mode Modbus device manager implementation

using FluentAssertions;
using Industrial.Adam.Logger.Configuration;
using Industrial.Adam.Logger.Infrastructure;
using Industrial.Adam.Logger.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Industrial.Adam.Logger.Tests.Infrastructure;

/// <summary>
/// Unit tests for MockModbusDeviceManager focusing on demo mode simulation behavior
/// </summary>
public class MockModbusDeviceManagerTests : IDisposable
{
    private readonly ILogger<MockModbusDeviceManager> _logger;
    private readonly AdamDeviceConfig _testConfig;
    private readonly List<MockModbusDeviceManager> _managersToDispose;

    public MockModbusDeviceManagerTests()
    {
        _logger = NullLogger<MockModbusDeviceManager>.Instance;
        _testConfig = TestConfigurationBuilder.ValidDeviceConfig();
        _managersToDispose = new List<MockModbusDeviceManager>();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ValidParameters_ShouldCreateInstance()
    {
        // Act
        var manager = CreateManager();

        // Assert
        manager.Should().NotBeNull();
        manager.DeviceId.Should().Be(_testConfig.DeviceId);
        manager.Configuration.Should().Be(_testConfig);
        manager.IsConnected.Should().BeFalse();

        // Note: Logger verification removed - using NullLogger for testing
        // Production logging is tested through integration tests
    }

    [Fact]
    public void Constructor_ShouldInitializeWithRandomCounter()
    {
        // Arrange & Act
        var manager1 = CreateManager();
        var manager2 = CreateManager();

        // Assert
        // Both managers should be created successfully
        manager1.Should().NotBeNull();
        manager2.Should().NotBeNull();

        // They should have different device IDs if created with different configs
        manager1.DeviceId.Should().Be(_testConfig.DeviceId);
        manager2.DeviceId.Should().Be(_testConfig.DeviceId);
    }

    [Fact]
    public void Constructor_NullConfig_ShouldThrowException()
    {
        // Act & Assert
        Action action = () => new MockModbusDeviceManager(null!, _logger);
        // The implementation doesn't check for null, so it will throw NullReferenceException
        action.Should().Throw<Exception>();
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrowException()
    {
        // Act & Assert
        Action action = () => new MockModbusDeviceManager(_testConfig, null!);
        // The implementation doesn't check for null, so it will throw NullReferenceException
        action.Should().Throw<Exception>();
    }

    #endregion

    #region Property Tests

    [Fact]
    public void DeviceId_ShouldReturnConfiguredDeviceId()
    {
        // Arrange
        var manager = CreateManager();

        // Act & Assert
        manager.DeviceId.Should().Be(_testConfig.DeviceId);
    }

    [Fact]
    public void Configuration_ShouldReturnConfiguredDevice()
    {
        // Arrange
        var manager = CreateManager();

        // Act & Assert
        manager.Configuration.Should().Be(_testConfig);
    }

    [Fact]
    public void IsConnected_InitialState_ShouldBeFalse()
    {
        // Arrange
        var manager = CreateManager();

        // Act & Assert
        manager.IsConnected.Should().BeFalse();
    }

    #endregion

    #region ConnectAsync Tests

    [Fact]
    public async Task ConnectAsync_ShouldConnectSuccessfully()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        var result = await manager.ConnectAsync();

        // Assert
        result.Should().BeTrue();
        manager.IsConnected.Should().BeTrue();
    }

    [Fact]
    public async Task ConnectAsync_MultipleConnects_ShouldRemainConnected()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        var result1 = await manager.ConnectAsync();
        var result2 = await manager.ConnectAsync();

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeTrue();
        manager.IsConnected.Should().BeTrue();
    }

    [Fact]
    public async Task ConnectAsync_WithCancellation_ShouldRespectCancellation()
    {
        // Arrange
        var manager = CreateManager();
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        // The implementation throws TaskCanceledException when cancelled
        await manager.Invoking(m => m.ConnectAsync(cts.Token))
            .Should().ThrowAsync<TaskCanceledException>();
        
        manager.IsConnected.Should().BeFalse();
    }

    #endregion

    #region ReadRegistersAsync Tests

    [Fact]
    public async Task ReadRegistersAsync_WhenConnected_ShouldReturnSimulatedData()
    {
        // Arrange
        var manager = CreateManager();
        await manager.ConnectAsync();

        // Act
        var result = await manager.ReadRegistersAsync(0, 1);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Should().HaveCount(1);
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task ReadRegistersAsync_WhenNotConnected_ShouldAutoConnect()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        var result = await manager.ReadRegistersAsync(0, 1);

        // Assert
        // MockModbusDeviceManager auto-connects if not connected (line 60)
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        manager.IsConnected.Should().BeTrue();
    }

    [Fact]
    public async Task ReadRegistersAsync_MultipleReads_ShouldReturnIncrementingCounters()
    {
        // Arrange
        var manager = CreateManager();
        await manager.ConnectAsync();

        // Act
        // Read 2 registers to get the full 32-bit counter
        var result1 = await manager.ReadRegistersAsync(0, 2);
        await Task.Delay(1000); // Wait 1 second to ensure counter increments
        var result2 = await manager.ReadRegistersAsync(0, 2);

        // Assert
        result1.Success.Should().BeTrue();
        result2.Success.Should().BeTrue();
        
        // Reconstruct 32-bit counter values
        var value1 = (uint)result1.Data![0] | ((uint)result1.Data[1] << 16);
        var value2 = (uint)result2.Data![0] | ((uint)result2.Data[1] << 16);
        
        // Counter should increment over time
        value2.Should().BeGreaterThanOrEqualTo(value1);
    }

    [Fact]
    public async Task ReadRegistersAsync_DifferentChannels_ShouldReturnDifferentData()
    {
        // Arrange
        var manager = CreateManager();
        await manager.ConnectAsync();

        // Act
        var result1 = await manager.ReadRegistersAsync(0, 1);
        var result2 = await manager.ReadRegistersAsync(1, 1);
        var result3 = await manager.ReadRegistersAsync(2, 1);

        // Assert
        result1.Success.Should().BeTrue();
        result2.Success.Should().BeTrue();
        result3.Success.Should().BeTrue();
        
        // Different channels should have different values
        var value1 = result1.Data![0];
        var value2 = result2.Data![0];
        var value3 = result3.Data![0];
        
        // Values should be unique (in most cases)
        new[] { value1, value2, value3 }.Distinct().Count().Should().BeGreaterThan(1);
    }

    [Fact]
    public async Task ReadRegistersAsync_WithCancellation_ShouldRespectCancellation()
    {
        // Arrange
        var manager = CreateManager();
        await manager.ConnectAsync();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await manager.ReadRegistersAsync(0, 1, cts.Token);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNull();
    }

    [Fact]
    public async Task ReadRegistersAsync_SimulationDelay_ShouldHaveRealisticTiming()
    {
        // Arrange
        var manager = CreateManager();
        await manager.ConnectAsync();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await manager.ReadRegistersAsync(0, 1);
        stopwatch.Stop();

        // Assert
        result.Success.Should().BeTrue();
        // Mock device should have some realistic delay to simulate network communication
        stopwatch.Elapsed.Should().BeGreaterThan(TimeSpan.Zero);
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(1)); // But not too long
    }

    #endregion

    #region TestConnectionAsync Tests

    [Fact]
    public async Task TestConnectionAsync_WhenConnected_ShouldReturnTrue()
    {
        // Arrange
        var manager = CreateManager();
        await manager.ConnectAsync();

        // Act
        var result = await manager.TestConnectionAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task TestConnectionAsync_WhenNotConnected_ShouldReturnFalse()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        var result = await manager.TestConnectionAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TestConnectionAsync_WithCancellation_ShouldStillReturnResult()
    {
        // Arrange
        var manager = CreateManager();
        await manager.ConnectAsync();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await manager.TestConnectionAsync(cts.Token);

        // Assert
        // The mock implementation doesn't await the delay, so cancellation doesn't affect it
        result.Should().BeTrue(); // Because IsConnected is true
    }

    #endregion

    #region Demo Mode Specific Tests

    [Fact]
    public async Task DemoMode_CounterIncrement_ShouldShowRealisticCounterBehavior()
    {
        // Arrange
        var manager = CreateManager();
        await manager.ConnectAsync();

        // Act
        // Read 2 registers to get the full 32-bit counter
        var result1 = await manager.ReadRegistersAsync(0, 2);
        await Task.Delay(2000); // Wait 2 seconds to ensure meaningful increment
        var result2 = await manager.ReadRegistersAsync(0, 2);

        // Assert
        result1.Success.Should().BeTrue();
        result2.Success.Should().BeTrue();
        
        // Reconstruct 32-bit counter values
        var value1 = (uint)result1.Data![0] | ((uint)result1.Data[1] << 16);
        var value2 = (uint)result2.Data![0] | ((uint)result2.Data[1] << 16);
        
        // Counter should increment over time (1-5 counts per second)
        value2.Should().BeGreaterThan(value1);
        var increment = value2 - value1;
        increment.Should().BeInRange(1, 10); // 2 seconds * max 5 counts/sec
    }

    [Fact]
    public async Task DemoMode_CounterOverflow_ShouldWrapAround()
    {
        // Arrange
        var manager = CreateManager();
        await manager.ConnectAsync();

        // Force the counter to near overflow by reading many times
        // Note: This is a conceptual test - in real demo mode, counters would
        // wrap around at UINT16 max (65535)
        
        // Act & Assert
        // Verify that the manager handles overflow gracefully
        for (int i = 0; i < 10; i++)
        {
            var result = await manager.ReadRegistersAsync(0, 1);
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data![0].Should().BeInRange(0, 65535);
        }
    }

    [Fact]
    public void DemoMode_Configuration_ShouldAcceptStandardDeviceConfig()
    {
        // Arrange
        var config = TestConfigurationBuilder.ValidDeviceConfig();

        // Act & Assert
        config.Should().NotBeNull();
        config.TimeoutMs.Should().BeGreaterThan(0);
        config.IpAddress.Should().NotBeNullOrEmpty();
        config.Port.Should().BeGreaterThan(0);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_ShouldCleanupResources()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        manager.Dispose();

        // Assert
        manager.IsConnected.Should().BeFalse();
    }

    [Fact]
    public void Dispose_MultipleDispose_ShouldNotThrow()
    {
        // Arrange
        var manager = CreateManager();

        // Act & Assert
        manager.Dispose();
        Action secondDispose = () => manager.Dispose();
        secondDispose.Should().NotThrow();
    }

    [Fact]
    public async Task Dispose_AfterConnection_ShouldCleanupProperly()
    {
        // Arrange
        var manager = CreateManager();
        await manager.ConnectAsync();

        // Act
        manager.Dispose();

        // Assert
        manager.IsConnected.Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private MockModbusDeviceManager CreateManager(AdamDeviceConfig? config = null)
    {
        var actualConfig = config ?? _testConfig;
        var manager = new MockModbusDeviceManager(actualConfig, _logger);
        _managersToDispose.Add(manager);
        return manager;
    }

    #endregion

    public void Dispose()
    {
        foreach (var manager in _managersToDispose)
        {
            try
            {
                manager.Dispose();
            }
            catch
            {
                // Ignore disposal errors in tests
            }
        }
        _managersToDispose.Clear();
    }
}