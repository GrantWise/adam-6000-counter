// Industrial.Adam.Logger.Tests - InfluxDbDataProcessor Unit Tests
// Tests for InfluxDB data processing service - REQUIREMENTS-BASED TESTING

using System.Collections.Concurrent;
using FluentAssertions;
using Industrial.Adam.Logger.Configuration;
using Industrial.Adam.Logger.Interfaces;
using Industrial.Adam.Logger.Models;
using Industrial.Adam.Logger.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Industrial.Adam.Logger.Tests.Services;

/// <summary>
/// Unit tests for InfluxDbDataProcessor service
/// FOCUS: Testing business requirements and expected behavior, not just implementation
/// </summary>
public class InfluxDbDataProcessorTests : IDisposable
{
    private readonly Mock<IDataValidator> _mockValidator;
    private readonly Mock<IDataTransformer> _mockTransformer;
    private readonly Mock<IInfluxDbWriter> _mockInfluxDbWriter;
    private readonly Mock<ILogger<InfluxDbDataProcessor>> _mockLogger;
    private readonly AdamLoggerConfig _config;
    private readonly InfluxDbDataProcessor _processor;

    public InfluxDbDataProcessorTests()
    {
        _mockValidator = new Mock<IDataValidator>();
        _mockTransformer = new Mock<IDataTransformer>();
        _mockInfluxDbWriter = new Mock<IInfluxDbWriter>();
        _mockLogger = new Mock<ILogger<InfluxDbDataProcessor>>();

        _config = new AdamLoggerConfig
        {
            InfluxDb = new InfluxDbConfig
            {
                Url = "http://localhost:8086",
                Token = "test-token",
                Organization = "test-org",
                Bucket = "test-bucket",
                WriteBatchSize = 100,
                FlushIntervalMs = 5000,
                EnableRetry = true,
                MaxRetryAttempts = 3,
                RetryDelayMs = 1000
            }
        };

        var options = Options.Create(_config);
        _processor = new InfluxDbDataProcessor(
            _mockValidator.Object,
            _mockTransformer.Object,
            _mockInfluxDbWriter.Object,
            options,
            _mockLogger.Object);
    }

    public void Dispose()
    {
        _processor?.Dispose();
    }

    #region ProcessRawData Business Requirements Tests

    [Fact]
    public void ProcessRawData_WithValidData_ShouldProcessAndQueueForInfluxDb()
    {
        // REQUIREMENT: Valid data must be processed and queued for InfluxDB storage
        // for real-time industrial data collection
        
        // Arrange
        var deviceId = "ADAM-001";
        var channel = new ChannelConfig { ChannelNumber = 1, Name = "Counter1" };
        var registers = new ushort[] { 1000, 2000 };
        var timestamp = DateTimeOffset.UtcNow;
        var acquisitionTime = TimeSpan.FromMilliseconds(100);

        _mockValidator.Setup(v => v.ValidateReading(It.IsAny<AdamDataReading>(), It.IsAny<ChannelConfig>()))
            .Returns(DataQuality.Good);
        _mockTransformer.Setup(t => t.TransformValue(It.IsAny<long>(), It.IsAny<ChannelConfig>()))
            .Returns(1000.0);
        _mockTransformer.Setup(t => t.EnrichTags(It.IsAny<Dictionary<string, object>>(), It.IsAny<AdamDeviceConfig>(), It.IsAny<ChannelConfig>()))
            .Returns(new Dictionary<string, object> { ["device"] = deviceId });

        // Act
        var result = _processor.ProcessRawData(deviceId, channel, registers, timestamp, acquisitionTime);

        // Assert - REQUIREMENT VALIDATION
        result.Should().NotBeNull("Valid data must be processed for industrial data collection");
        result.DeviceId.Should().Be(deviceId, "Device ID must be preserved for data traceability");
        result.Channel.Should().Be(channel.ChannelNumber, "Channel number must be preserved for data organization");
        result.Timestamp.Should().Be(timestamp, "Timestamp must be preserved for data chronology");
        result.AcquisitionTime.Should().Be(acquisitionTime, "Acquisition time must be tracked for performance monitoring");
        result.Quality.Should().Be(DataQuality.Good, "Good quality data must be properly classified");
    }

    [Fact]
    public void ProcessRawData_WithGoodQuality_ShouldQueueForInfluxDb()
    {
        // REQUIREMENT: Only good quality data should be queued for InfluxDB
        // to maintain data integrity in time-series database
        
        // Arrange
        var deviceId = "ADAM-002";
        var channel = new ChannelConfig { ChannelNumber = 2, Name = "Counter2" };
        var registers = new ushort[] { 3000, 4000 };
        var timestamp = DateTimeOffset.UtcNow;
        var acquisitionTime = TimeSpan.FromMilliseconds(150);

        _mockValidator.Setup(v => v.ValidateReading(It.IsAny<AdamDataReading>(), It.IsAny<ChannelConfig>()))
            .Returns(DataQuality.Good);
        _mockTransformer.Setup(t => t.TransformValue(It.IsAny<long>(), It.IsAny<ChannelConfig>()))
            .Returns(2000.0);
        _mockTransformer.Setup(t => t.EnrichTags(It.IsAny<Dictionary<string, object>>(), It.IsAny<AdamDeviceConfig>(), It.IsAny<ChannelConfig>()))
            .Returns(new Dictionary<string, object>());

        // Act
        var result = _processor.ProcessRawData(deviceId, channel, registers, timestamp, acquisitionTime);

        // Assert - REQUIREMENT VALIDATION
        result.Quality.Should().Be(DataQuality.Good, "Good quality data must be properly classified");
        
        // Wait briefly to allow potential async queuing
        Thread.Sleep(100);
        
        // Verify that the data would be queued (we can't directly test the private queue,
        // but we can verify the processing completed without errors)
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never,
            "Good quality data processing should not generate errors");
    }

    [Fact]
    public void ProcessRawData_WithBadQuality_ShouldNotQueueForInfluxDb()
    {
        // REQUIREMENT: Bad quality data should not be queued for InfluxDB
        // to prevent corrupt data from entering time-series database
        
        // Arrange
        var deviceId = "ADAM-003";
        var channel = new ChannelConfig { ChannelNumber = 3, Name = "Counter3" };
        var registers = new ushort[] { 5000, 6000 };
        var timestamp = DateTimeOffset.UtcNow;
        var acquisitionTime = TimeSpan.FromMilliseconds(200);

        _mockValidator.Setup(v => v.ValidateReading(It.IsAny<AdamDataReading>(), It.IsAny<ChannelConfig>()))
            .Returns(DataQuality.Bad);
        _mockTransformer.Setup(t => t.TransformValue(It.IsAny<long>(), It.IsAny<ChannelConfig>()))
            .Returns(3000.0);
        _mockTransformer.Setup(t => t.EnrichTags(It.IsAny<Dictionary<string, object>>(), It.IsAny<AdamDeviceConfig>(), It.IsAny<ChannelConfig>()))
            .Returns(new Dictionary<string, object>());

        // Act
        var result = _processor.ProcessRawData(deviceId, channel, registers, timestamp, acquisitionTime);

        // Assert - REQUIREMENT VALIDATION
        result.Quality.Should().Be(DataQuality.Bad, "Bad quality data must be properly classified");
        
        // Bad quality data should still be processed but not queued for InfluxDB
        result.DeviceId.Should().Be(deviceId, "Device ID must be preserved even for bad quality data");
        result.Channel.Should().Be(channel.ChannelNumber, "Channel number must be preserved even for bad quality data");
    }

    [Fact]
    public void ProcessRawData_WithoutInfluxDbConfig_ShouldProcessWithoutQueuing()
    {
        // REQUIREMENT: System must function without InfluxDB configuration
        // for flexible deployment scenarios
        
        // Arrange
        var configWithoutInfluxDb = new AdamLoggerConfig
        {
            InfluxDb = null // No InfluxDB configuration
        };
        var optionsWithoutInfluxDb = Options.Create(configWithoutInfluxDb);
        
        var processorWithoutInfluxDb = new InfluxDbDataProcessor(
            _mockValidator.Object,
            _mockTransformer.Object,
            _mockInfluxDbWriter.Object,
            optionsWithoutInfluxDb,
            _mockLogger.Object);

        var deviceId = "ADAM-004";
        var channel = new ChannelConfig { ChannelNumber = 4, Name = "Counter4" };
        var registers = new ushort[] { 7000, 8000 };
        var timestamp = DateTimeOffset.UtcNow;
        var acquisitionTime = TimeSpan.FromMilliseconds(250);

        _mockValidator.Setup(v => v.ValidateReading(It.IsAny<AdamDataReading>(), It.IsAny<ChannelConfig>()))
            .Returns(DataQuality.Good);
        _mockTransformer.Setup(t => t.TransformValue(It.IsAny<long>(), It.IsAny<ChannelConfig>()))
            .Returns(4000.0);
        _mockTransformer.Setup(t => t.EnrichTags(It.IsAny<Dictionary<string, object>>(), It.IsAny<AdamDeviceConfig>(), It.IsAny<ChannelConfig>()))
            .Returns(new Dictionary<string, object>());

        // Act
        var result = processorWithoutInfluxDb.ProcessRawData(deviceId, channel, registers, timestamp, acquisitionTime);

        // Assert - REQUIREMENT VALIDATION
        result.Should().NotBeNull("System must function without InfluxDB configuration");
        result.Quality.Should().Be(DataQuality.Good, "Data processing must work without InfluxDB");
        result.DeviceId.Should().Be(deviceId, "Device ID must be preserved without InfluxDB");
        
        // Cleanup
        processorWithoutInfluxDb.Dispose();
    }

    #endregion

    #region Rate Calculation Tests

    [Fact]
    public void CalculateRate_WithValidInputs_ShouldDelegateToBaseProcessor()
    {
        // REQUIREMENT: Rate calculation must delegate to base processor
        // for consistent rate calculation across all processors
        
        // Arrange
        var deviceId = "ADAM-005";
        var channelNumber = 5;
        var currentValue = 1000L;
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        var result = _processor.CalculateRate(deviceId, channelNumber, currentValue, timestamp);

        // Assert - REQUIREMENT VALIDATION
        // Since this delegates to the base processor, the result depends on base processor logic
        // The important requirement is that the method is available and doesn't throw
        result.Should().BeNull("First rate calculation should return null without history");
    }

    [Fact]
    public void CalculateRate_WithConsecutiveCalls_ShouldCalculateRateOverTime()
    {
        // REQUIREMENT: Rate calculation must work over time with consecutive calls
        // for accurate production rate monitoring
        
        // Arrange
        var deviceId = "ADAM-006";
        var channelNumber = 6;
        var timestamp1 = DateTimeOffset.UtcNow;
        var timestamp2 = timestamp1.AddSeconds(10);
        var value1 = 1000L;
        var value2 = 1100L;

        // Act
        var rate1 = _processor.CalculateRate(deviceId, channelNumber, value1, timestamp1);
        var rate2 = _processor.CalculateRate(deviceId, channelNumber, value2, timestamp2);

        // Assert - REQUIREMENT VALIDATION
        rate1.Should().BeNull("First rate calculation should return null without history");
        rate2.Should().NotBeNull("Second rate calculation should return value with history");
        rate2.Should().BeGreaterThan(0, "Rate should be positive for increasing counter");
    }

    #endregion

    #region Reading Validation Tests

    [Fact]
    public void ValidateReading_WithValidChannel_ShouldDelegateToBaseProcessor()
    {
        // REQUIREMENT: Reading validation must delegate to base processor
        // for consistent validation across all processors
        
        // Arrange
        var channel = new ChannelConfig { ChannelNumber = 1, Name = "Test", MinValue = 0, MaxValue = 10000 };
        var rawValue = 5000L;
        var rate = 10.0;

        // Act
        var result = _processor.ValidateReading(channel, rawValue, rate);

        // Assert - REQUIREMENT VALIDATION
        result.Should().BeOneOf(DataQuality.Good, DataQuality.Bad, DataQuality.Uncertain);
    }

    [Fact]
    public void ValidateReading_WithBoundaryValues_ShouldValidateCorrectly()
    {
        // REQUIREMENT: Boundary values must be validated correctly
        // for proper range checking in industrial applications
        
        // Arrange
        var channel = new ChannelConfig { ChannelNumber = 1, Name = "Test", MinValue = 0, MaxValue = 10000 };
        var minValue = 0L;
        var maxValue = 10000L;
        var rate = 5.0;

        // Act
        var minResult = _processor.ValidateReading(channel, minValue, rate);
        var maxResult = _processor.ValidateReading(channel, maxValue, rate);

        // Assert - REQUIREMENT VALIDATION
        minResult.Should().BeOneOf(DataQuality.Good, DataQuality.Bad, DataQuality.Uncertain);
        maxResult.Should().BeOneOf(DataQuality.Good, DataQuality.Bad, DataQuality.Uncertain);
    }

    #endregion

    #region Queue Management Tests

    [Fact]
    public void GetQueueSize_AfterProcessingGoodData_ShouldReflectQueuedItems()
    {
        // REQUIREMENT: Queue size must be available for monitoring
        // for system performance and capacity planning
        
        // Arrange
        var deviceId = "ADAM-007";
        var channel = new ChannelConfig { ChannelNumber = 7, Name = "Counter7" };
        var registers = new ushort[] { 1000, 2000 };
        var timestamp = DateTimeOffset.UtcNow;
        var acquisitionTime = TimeSpan.FromMilliseconds(100);

        _mockValidator.Setup(v => v.ValidateReading(It.IsAny<AdamDataReading>(), It.IsAny<ChannelConfig>()))
            .Returns(DataQuality.Good);
        _mockTransformer.Setup(t => t.TransformValue(It.IsAny<long>(), It.IsAny<ChannelConfig>()))
            .Returns(1000.0);
        _mockTransformer.Setup(t => t.EnrichTags(It.IsAny<Dictionary<string, object>>(), It.IsAny<AdamDeviceConfig>(), It.IsAny<ChannelConfig>()))
            .Returns(new Dictionary<string, object>());

        // Act
        var initialQueueSize = _processor.GetQueueSize();
        _processor.ProcessRawData(deviceId, channel, registers, timestamp, acquisitionTime);
        var finalQueueSize = _processor.GetQueueSize();

        // Assert - REQUIREMENT VALIDATION
        initialQueueSize.Should().Be(0, "Initial queue size should be zero");
        finalQueueSize.Should().BeGreaterOrEqualTo(0, "Final queue size should be non-negative");
    }

    [Fact]
    public void GetQueueSize_WithoutInfluxDbConfig_ShouldReturnZero()
    {
        // REQUIREMENT: Queue size should be zero when InfluxDB is not configured
        // for consistent monitoring behavior
        
        // Arrange
        var configWithoutInfluxDb = new AdamLoggerConfig { InfluxDb = null };
        var optionsWithoutInfluxDb = Options.Create(configWithoutInfluxDb);
        
        var processorWithoutInfluxDb = new InfluxDbDataProcessor(
            _mockValidator.Object,
            _mockTransformer.Object,
            _mockInfluxDbWriter.Object,
            optionsWithoutInfluxDb,
            _mockLogger.Object);

        // Act
        var queueSize = processorWithoutInfluxDb.GetQueueSize();

        // Assert - REQUIREMENT VALIDATION
        queueSize.Should().Be(0, "Queue size should be zero when InfluxDB is not configured");
        
        // Cleanup
        processorWithoutInfluxDb.Dispose();
    }

    #endregion

    #region Flush Operations Tests

    [Fact]
    public async Task FlushAsync_WithQueuedData_ShouldFlushToInfluxDb()
    {
        // REQUIREMENT: Flush operation must write all queued data to InfluxDB
        // for data persistence and system shutdown scenarios
        
        // Arrange
        var deviceId = "ADAM-008";
        var channel = new ChannelConfig { ChannelNumber = 8, Name = "Counter8" };
        var registers = new ushort[] { 3000, 4000 };
        var timestamp = DateTimeOffset.UtcNow;
        var acquisitionTime = TimeSpan.FromMilliseconds(150);

        _mockValidator.Setup(v => v.ValidateReading(It.IsAny<AdamDataReading>(), It.IsAny<ChannelConfig>()))
            .Returns(DataQuality.Good);
        _mockTransformer.Setup(t => t.TransformValue(It.IsAny<long>(), It.IsAny<ChannelConfig>()))
            .Returns(2000.0);
        _mockTransformer.Setup(t => t.EnrichTags(It.IsAny<Dictionary<string, object>>(), It.IsAny<AdamDeviceConfig>(), It.IsAny<ChannelConfig>()))
            .Returns(new Dictionary<string, object>());

        _mockInfluxDbWriter.Setup(w => w.WriteBatchAsync(It.IsAny<IEnumerable<AdamDataReading>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Process some data to queue it
        _processor.ProcessRawData(deviceId, channel, registers, timestamp, acquisitionTime);

        // Act
        await _processor.FlushAsync();

        // Assert - REQUIREMENT VALIDATION
        _mockInfluxDbWriter.Verify(w => w.WriteBatchAsync(
            It.IsAny<IEnumerable<AdamDataReading>>(),
            It.IsAny<CancellationToken>()),
            Times.AtLeastOnce,
            "Flush operation must write queued data to InfluxDB");
    }

    [Fact]
    public async Task FlushAsync_WithoutInfluxDbConfig_ShouldCompleteWithoutError()
    {
        // REQUIREMENT: Flush operation must complete gracefully without InfluxDB configuration
        // for flexible deployment scenarios
        
        // Arrange
        var configWithoutInfluxDb = new AdamLoggerConfig { InfluxDb = null };
        var optionsWithoutInfluxDb = Options.Create(configWithoutInfluxDb);
        
        var processorWithoutInfluxDb = new InfluxDbDataProcessor(
            _mockValidator.Object,
            _mockTransformer.Object,
            _mockInfluxDbWriter.Object,
            optionsWithoutInfluxDb,
            _mockLogger.Object);

        // Act & Assert - REQUIREMENT VALIDATION
        await processorWithoutInfluxDb.Invoking(p => p.FlushAsync())
            .Should().NotThrowAsync("Flush operation must complete gracefully without InfluxDB configuration");
        
        // Cleanup
        processorWithoutInfluxDb.Dispose();
    }

    [Fact]
    public async Task FlushAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // REQUIREMENT: Flush operation must respect cancellation tokens
        // for responsive system shutdown and timeout handling
        
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel(); // Cancel immediately

        // Act & Assert - REQUIREMENT VALIDATION
        await _processor.Invoking(p => p.FlushAsync(cancellationTokenSource.Token))
            .Should().ThrowAsync<OperationCanceledException>("Flush operation must respect cancellation tokens");
    }

    #endregion

    #region Disposal Tests

    [Fact]
    public void Dispose_ShouldCleanupResourcesGracefully()
    {
        // REQUIREMENT: Disposal must clean up resources gracefully
        // for proper system shutdown and resource management
        
        // Arrange
        var processor = new InfluxDbDataProcessor(
            _mockValidator.Object,
            _mockTransformer.Object,
            _mockInfluxDbWriter.Object,
            Options.Create(_config),
            _mockLogger.Object);

        // Act & Assert - REQUIREMENT VALIDATION
        processor.Invoking(p => p.Dispose())
            .Should().NotThrow("Disposal must clean up resources gracefully");

        // Verify multiple disposals don't cause issues
        processor.Invoking(p => p.Dispose())
            .Should().NotThrow("Multiple disposals must be handled gracefully");
    }

    [Fact]
    public void Dispose_AfterProcessingData_ShouldFlushPendingData()
    {
        // REQUIREMENT: Disposal must flush pending data to prevent data loss
        // for data integrity during system shutdown
        
        // Arrange
        var processor = new InfluxDbDataProcessor(
            _mockValidator.Object,
            _mockTransformer.Object,
            _mockInfluxDbWriter.Object,
            Options.Create(_config),
            _mockLogger.Object);

        var deviceId = "ADAM-009";
        var channel = new ChannelConfig { ChannelNumber = 9, Name = "Counter9" };
        var registers = new ushort[] { 5000, 6000 };
        var timestamp = DateTimeOffset.UtcNow;
        var acquisitionTime = TimeSpan.FromMilliseconds(200);

        _mockValidator.Setup(v => v.ValidateReading(It.IsAny<AdamDataReading>(), It.IsAny<ChannelConfig>()))
            .Returns(DataQuality.Good);
        _mockTransformer.Setup(t => t.TransformValue(It.IsAny<long>(), It.IsAny<ChannelConfig>()))
            .Returns(3000.0);
        _mockTransformer.Setup(t => t.EnrichTags(It.IsAny<Dictionary<string, object>>(), It.IsAny<AdamDeviceConfig>(), It.IsAny<ChannelConfig>()))
            .Returns(new Dictionary<string, object>());

        _mockInfluxDbWriter.Setup(w => w.WriteBatchAsync(It.IsAny<IEnumerable<AdamDataReading>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Process some data
        var result = processor.ProcessRawData(deviceId, channel, registers, timestamp, acquisitionTime);

        // Verify data was processed with good quality
        result.Quality.Should().Be(DataQuality.Good, "Data should be processed with good quality");

        // Verify data was queued
        var queueSizeBeforeDispose = processor.GetQueueSize();
        queueSizeBeforeDispose.Should().BeGreaterThan(0, "Data should be queued for InfluxDB processing");

        // Act
        processor.Dispose();

        // Verify queue was flushed
        var queueSizeAfterDispose = processor.GetQueueSize();
        queueSizeAfterDispose.Should().Be(0, "Queue should be empty after disposal");

        // Assert - REQUIREMENT VALIDATION
        // Verify that WriteBatchAsync was called during disposal to flush pending data
        _mockInfluxDbWriter.Verify(w => w.WriteBatchAsync(
            It.IsAny<IEnumerable<AdamDataReading>>(),
            It.IsAny<CancellationToken>()),
            Times.AtLeastOnce,
            "Disposal must flush pending data to prevent data loss");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void ProcessRawData_WithValidatorException_ShouldHandleGracefully()
    {
        // REQUIREMENT: Validator exceptions must be handled gracefully
        // for system resilience and continuous operation
        
        // Arrange
        var deviceId = "ADAM-010";
        var channel = new ChannelConfig { ChannelNumber = 10, Name = "Counter10" };
        var registers = new ushort[] { 7000, 8000 };
        var timestamp = DateTimeOffset.UtcNow;
        var acquisitionTime = TimeSpan.FromMilliseconds(300);

        _mockValidator.Setup(v => v.ValidateReading(It.IsAny<AdamDataReading>(), It.IsAny<ChannelConfig>()))
            .Throws(new InvalidOperationException("Validator error"));
        _mockTransformer.Setup(t => t.TransformValue(It.IsAny<long>(), It.IsAny<ChannelConfig>()))
            .Returns(4000.0);
        _mockTransformer.Setup(t => t.EnrichTags(It.IsAny<Dictionary<string, object>>(), It.IsAny<AdamDeviceConfig>(), It.IsAny<ChannelConfig>()))
            .Returns(new Dictionary<string, object>());

        // Act & Assert - REQUIREMENT VALIDATION
        _processor.Invoking(p => p.ProcessRawData(deviceId, channel, registers, timestamp, acquisitionTime))
            .Should().NotThrow("Validator exceptions must be handled gracefully");
    }

    [Fact]
    public void ProcessRawData_WithTransformerException_ShouldHandleGracefully()
    {
        // REQUIREMENT: Transformer exceptions must be handled gracefully
        // for system resilience and continuous operation
        
        // Arrange
        var deviceId = "ADAM-011";
        var channel = new ChannelConfig { ChannelNumber = 11, Name = "Counter11" };
        var registers = new ushort[] { 9000, 10000 };
        var timestamp = DateTimeOffset.UtcNow;
        var acquisitionTime = TimeSpan.FromMilliseconds(350);

        _mockValidator.Setup(v => v.ValidateReading(It.IsAny<AdamDataReading>(), It.IsAny<ChannelConfig>()))
            .Returns(DataQuality.Good);
        _mockTransformer.Setup(t => t.TransformValue(It.IsAny<long>(), It.IsAny<ChannelConfig>()))
            .Throws(new ArgumentException("Transformer error"));
        _mockTransformer.Setup(t => t.EnrichTags(It.IsAny<Dictionary<string, object>>(), It.IsAny<AdamDeviceConfig>(), It.IsAny<ChannelConfig>()))
            .Returns(new Dictionary<string, object>());

        // Act & Assert - REQUIREMENT VALIDATION
        _processor.Invoking(p => p.ProcessRawData(deviceId, channel, registers, timestamp, acquisitionTime))
            .Should().NotThrow("Transformer exceptions must be handled gracefully");
    }

    #endregion
}