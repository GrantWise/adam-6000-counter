// Industrial.Adam.Logger.Tests - CounterMetricsCollector Unit Tests
// Comprehensive tests for high-performance industrial counter metrics collection

using FluentAssertions;
using Industrial.Adam.Logger.Configuration;
using Industrial.Adam.Logger.Models;
using Industrial.Adam.Logger.Monitoring;
using Industrial.Adam.Logger.Performance;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Industrial.Adam.Logger.Tests.Monitoring;

public class CounterMetricsCollectorTests : IDisposable
{
    private readonly Mock<ILogger<CounterMetricsCollector>> _mockLogger;
    private readonly Mock<IOptions<AdamLoggerConfig>> _mockOptions;
    private readonly AdamLoggerConfig _config;
    private readonly CounterMetricsCollector _collector;

    public CounterMetricsCollectorTests()
    {
        _mockLogger = new Mock<ILogger<CounterMetricsCollector>>();
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
        _collector = new CounterMetricsCollector(_mockOptions.Object, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_ValidParameters_ShouldCreateInstance()
    {
        // Arrange
        var freshMockLogger = new Mock<ILogger<CounterMetricsCollector>>();
        
        // Act
        var collector = new CounterMetricsCollector(_mockOptions.Object, freshMockLogger.Object);

        // Assert
        collector.Should().NotBeNull();
        freshMockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Counter metrics collector initialized")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_NullConfig_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var action = () => new CounterMetricsCollector(null!, _mockLogger.Object);
        action.Should().Throw<ArgumentNullException>().WithParameterName("config");
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var action = () => new CounterMetricsCollector(_mockOptions.Object, null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void RecordCounterReading_ValidReading_ShouldUpdateMetrics()
    {
        // Arrange
        var reading = new AdamDataReading
        {
            DeviceId = "test-device",
            Channel = 0,
            RawValue = 1000,
            Timestamp = DateTimeOffset.UtcNow,
            ProcessedValue = 100.0,
            Quality = DataQuality.Good
        };
        var processingTime = TimeSpan.FromMilliseconds(50);

        // Act
        _collector.RecordCounterReading(reading, processingTime);

        // Assert - Should not throw and should log no errors
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public void RecordCounterReading_BadQualityReading_ShouldUpdateFailureCounters()
    {
        // Arrange
        var reading = new AdamDataReading
        {
            DeviceId = "test-device",
            Channel = 0,
            RawValue = 1000,
            Timestamp = DateTimeOffset.UtcNow,
            ProcessedValue = 100.0,
            Quality = DataQuality.Bad
        };
        var processingTime = TimeSpan.FromMilliseconds(50);

        // Act
        _collector.RecordCounterReading(reading, processingTime);

        // Assert - Should not throw and should log no errors
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public void RecordCounterReading_MultipleReadings_ShouldAggregateMetrics()
    {
        // Arrange
        var readings = new[]
        {
            new AdamDataReading
            {
                DeviceId = "test-device",
                Channel = 0,
                RawValue = 1000,
                Timestamp = DateTimeOffset.UtcNow,
                ProcessedValue = 100.0,
                Quality = DataQuality.Good
            },
            new AdamDataReading
            {
                DeviceId = "test-device",
                Channel = 0,
                RawValue = 2000,
                Timestamp = DateTimeOffset.UtcNow,
                ProcessedValue = 200.0,
                Quality = DataQuality.Good
            }
        };

        // Act
        foreach (var reading in readings)
        {
            _collector.RecordCounterReading(reading, TimeSpan.FromMilliseconds(50));
        }

        // Assert - Should not throw and should log no errors
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public void RecordDeviceConnectivity_ValidParameters_ShouldUpdateDeviceMetrics()
    {
        // Arrange
        var deviceId = "test-device";
        var isConnected = true;
        var responseTime = TimeSpan.FromMilliseconds(100);

        // Act
        _collector.RecordDeviceConnectivity(deviceId, isConnected, responseTime);

        // Assert - Should not throw and should log no errors
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public void RecordDeviceConnectivity_DeviceDisconnected_ShouldUpdateStatus()
    {
        // Arrange
        var deviceId = "test-device";
        var isConnected = false;
        var responseTime = TimeSpan.FromMilliseconds(5000);

        // Act
        _collector.RecordDeviceConnectivity(deviceId, isConnected, responseTime);

        // Assert - Should not throw and should log no errors
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public void RecordDataQuality_ValidParameters_ShouldUpdateQualityMetrics()
    {
        // Arrange
        var deviceId = "test-device";
        var channelNumber = 0;
        var quality = DataQuality.Good;
        var validationTime = TimeSpan.FromMilliseconds(10);

        // Act
        _collector.RecordDataQuality(deviceId, channelNumber, quality, validationTime);

        // Assert - Should not throw and should log no errors
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public void RecordDataQuality_BadQuality_ShouldUpdateBadQualityCounters()
    {
        // Arrange
        var deviceId = "test-device";
        var channelNumber = 0;
        var quality = DataQuality.Bad;
        var validationTime = TimeSpan.FromMilliseconds(10);

        // Act
        _collector.RecordDataQuality(deviceId, channelNumber, quality, validationTime);

        // Assert - Should not throw and should log no errors
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public void RecordPerformanceMetrics_ValidMetrics_ShouldLogDebugMessage()
    {
        // Arrange
        var metrics = new PerformanceMetrics
        {
            CpuUsagePercent = 25.5,
            MemoryUsageMB = 1024,
            DataProcessingRatePerSecond = 100.0,
            AverageResponseTimeMs = 50.0,
            ActiveConnections = 5,
            QueuedOperations = 10,
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        _collector.RecordPerformanceMetrics(metrics);

        // Assert - Should log debug message
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Performance metrics recorded")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordMemoryMetrics_ValidMetrics_ShouldLogDebugMessage()
    {
        // Arrange
        var metrics = new MemoryMetrics
        {
            TotalMemoryMB = 2048,
            UsedMemoryMB = 1024,
            AvailableMemoryMB = 1024,
            MemoryUsagePercent = 50.0,
            GarbageCollectionCount = 5,
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        _collector.RecordMemoryMetrics(metrics);

        // Assert - Should log debug message
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Memory metrics recorded")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordNetworkMetrics_ValidMetrics_ShouldLogDebugMessage()
    {
        // Arrange
        var metrics = new NetworkMetrics
        {
            AverageLatencyMs = 25.0,
            ThroughputMbps = 100.0,
            ActiveConnections = 10,
            FailedConnections = 2,
            PacketLossPercent = 0.5,
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        _collector.RecordNetworkMetrics(metrics);

        // Assert - Should log debug message
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Network metrics recorded")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordRateCalculation_ValidParameters_ShouldUpdateRateMetrics()
    {
        // Arrange
        var deviceId = "test-device";
        var channelNumber = 0;
        var calculatedRate = 15.5;
        var dataPoints = 10;
        var calculationTime = TimeSpan.FromMilliseconds(5);

        // Act
        _collector.RecordRateCalculation(deviceId, channelNumber, calculatedRate, dataPoints, calculationTime);

        // Assert - Should log trace message
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Trace,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Rate calculation recorded")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordRateCalculation_NullRate_ShouldHandleGracefully()
    {
        // Arrange
        var deviceId = "test-device";
        var channelNumber = 0;
        double? calculatedRate = null;
        var dataPoints = 10;
        var calculationTime = TimeSpan.FromMilliseconds(5);

        // Act
        _collector.RecordRateCalculation(deviceId, channelNumber, calculatedRate, dataPoints, calculationTime);

        // Assert - Should not throw and should log trace message
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Trace,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Rate calculation recorded")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordCounterOverflow_ValidParameters_ShouldUpdateOverflowMetrics()
    {
        // Arrange
        var deviceId = "test-device";
        var channelNumber = 0;
        var previousValue = 4294967295L; // Max uint32
        var currentValue = 100L;
        var adjustedValue = 4294967395L; // previousValue + currentValue

        // Act
        _collector.RecordCounterOverflow(deviceId, channelNumber, previousValue, currentValue, adjustedValue);

        // Assert - Should log information message
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Counter overflow detected")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordBatchProcessing_ValidParameters_ShouldUpdateBatchMetrics()
    {
        // Arrange
        var batchSize = 50;
        var processingTime = TimeSpan.FromMilliseconds(100);
        var successCount = 48;
        var failureCount = 2;

        // Act
        _collector.RecordBatchProcessing(batchSize, processingTime, successCount, failureCount);

        // Assert - Should log debug message
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Batch processing recorded")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetCurrentMetricsAsync_ShouldReturnValidMetricsSnapshot()
    {
        // Arrange
        var reading = new AdamDataReading
        {
            DeviceId = "test-device",
            Channel = 0,
            RawValue = 1000,
            Timestamp = DateTimeOffset.UtcNow,
            ProcessedValue = 100.0,
            Quality = DataQuality.Good
        };
        _collector.RecordCounterReading(reading, TimeSpan.FromMilliseconds(50));

        // Act
        var metrics = await _collector.GetCurrentMetricsAsync();

        // Assert
        metrics.Should().NotBeNull();
        metrics.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        metrics.Performance.Should().NotBeNull();
        metrics.Memory.Should().NotBeNull();
        metrics.Network.Should().NotBeNull();
        metrics.SystemHealth.Should().NotBeNull();
        metrics.Devices.Should().NotBeNull();
        metrics.Uptime.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task GetCounterMetricsAsync_ShouldReturnValidCounterMetricsSnapshot()
    {
        // Arrange
        var reading = new AdamDataReading
        {
            DeviceId = "test-device",
            Channel = 0,
            RawValue = 1000,
            Timestamp = DateTimeOffset.UtcNow,
            ProcessedValue = 100.0,
            Quality = DataQuality.Good
        };
        _collector.RecordCounterReading(reading, TimeSpan.FromMilliseconds(50));

        // Act
        var metrics = await _collector.GetCounterMetricsAsync();

        // Assert
        metrics.Should().NotBeNull();
        metrics.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        metrics.CounterChannels.Should().NotBeNull();
        metrics.OverflowEvents.Should().NotBeNull();
        metrics.RateCalculations.Should().NotBeNull();
        metrics.BatchProcessing.Should().NotBeNull();
        metrics.DataQuality.Should().NotBeNull();
    }

    [Fact]
    public void ResetMetrics_ShouldClearAllCounters()
    {
        // Arrange
        var reading = new AdamDataReading
        {
            DeviceId = "test-device",
            Channel = 0,
            RawValue = 1000,
            Timestamp = DateTimeOffset.UtcNow,
            ProcessedValue = 100.0,
            Quality = DataQuality.Good
        };
        _collector.RecordCounterReading(reading, TimeSpan.FromMilliseconds(50));

        // Act
        _collector.ResetMetrics();

        // Assert - Should log information message
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("All metrics counters reset")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Dispose_ShouldLogFinalStats()
    {
        // Arrange
        var reading = new AdamDataReading
        {
            DeviceId = "test-device",
            Channel = 0,
            RawValue = 1000,
            Timestamp = DateTimeOffset.UtcNow,
            ProcessedValue = 100.0,
            Quality = DataQuality.Good
        };
        _collector.RecordCounterReading(reading, TimeSpan.FromMilliseconds(50));

        // Act
        _collector.Dispose();

        // Assert - Should log information message with final stats
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Counter metrics collector disposed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Dispose_MultipleDispose_ShouldNotThrow()
    {
        // Act & Assert
        _collector.Dispose(); // Should not throw
        _collector.Dispose(); // Should not throw (idempotent)
    }

    [Fact]
    public void RecordCounterReading_ConcurrentCalls_ShouldBeThreadSafe()
    {
        // Arrange
        var tasks = new List<Task>();
        const int concurrentCalls = 100;

        // Act
        for (int i = 0; i < concurrentCalls; i++)
        {
            var reading = new AdamDataReading
            {
                DeviceId = $"device-{i % 10}",
                Channel = i % 4,
                RawValue = 1000 + i,
                Timestamp = DateTimeOffset.UtcNow,
                ProcessedValue = 100.0 + i,
                Quality = i % 10 == 0 ? DataQuality.Bad : DataQuality.Good
            };

            tasks.Add(Task.Run(() => _collector.RecordCounterReading(reading, TimeSpan.FromMilliseconds(50))));
        }

        // Assert
        var action = async () => await Task.WhenAll(tasks);
        action.Should().NotThrowAsync();
    }

    public void Dispose()
    {
        _collector?.Dispose();
    }
}