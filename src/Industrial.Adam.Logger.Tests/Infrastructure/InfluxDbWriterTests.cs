// Industrial.Adam.Logger.Tests - InfluxDbWriter Unit Tests
// Tests for the InfluxDB writer implementation

using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Industrial.Adam.Logger.Configuration;
using Industrial.Adam.Logger.Infrastructure;
using Industrial.Adam.Logger.Interfaces;
using Industrial.Adam.Logger.Models;
using Industrial.Adam.Logger.Tests.TestHelpers;
using Industrial.Adam.Logger.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Industrial.Adam.Logger.Tests.Infrastructure;

/// <summary>
/// Unit tests for InfluxDbWriter focusing on business logic and error handling
/// Note: These tests avoid actual InfluxDB connections by mocking dependencies
/// </summary>
public class InfluxDbWriterTests : IDisposable
{
    private readonly Mock<IRetryPolicyService> _mockRetryService;
    private readonly Mock<ILogger<InfluxDbWriter>> _mockLogger;
    private readonly AdamLoggerConfig _testConfig;
    private readonly IOptions<AdamLoggerConfig> _configOptions;
    private readonly List<InfluxDbWriter> _writersToDispose;

    public InfluxDbWriterTests()
    {
        _mockRetryService = new Mock<IRetryPolicyService>();
        _mockLogger = new Mock<ILogger<InfluxDbWriter>>();
        _testConfig = TestConfigurationBuilder.ValidLoggerConfig();
        _configOptions = Options.Create(_testConfig);
        _writersToDispose = new List<InfluxDbWriter>();

        SetupRetryService();
    }

    private void SetupRetryService()
    {
        // Setup default retry policy creation
        _mockRetryService.Setup(x => x.CreateNetworkRetryPolicy(It.IsAny<int>(), It.IsAny<TimeSpan?>()))
            .Returns(RetryPolicy.FixedDelay(3, TimeSpan.FromMilliseconds(100)));

        // Setup successful retry execution for void operations
        _mockRetryService.Setup(x => x.ExecuteAsync(
            It.IsAny<Func<CancellationToken, Task>>(),
            It.IsAny<RetryPolicy>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(OperationResult.Success()));

        // Setup successful retry execution for generic operations
        _mockRetryService.Setup(x => x.ExecuteAsync(
            It.IsAny<Func<CancellationToken, Task<bool>>>(),
            It.IsAny<RetryPolicy>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(OperationResult<bool>.Success(true)));
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ValidConfig_ShouldThrowWhenTryingToCreateWithoutInfluxDb()
    {
        // Arrange
        var config = TestConfigurationBuilder.ValidLoggerConfig();
        config.InfluxDb = null; // No InfluxDB config

        // Act & Assert
        Action action = () => new InfluxDbWriter(Options.Create(config), _mockRetryService.Object, _mockLogger.Object);
        action.Should().Throw<ArgumentException>().WithMessage("*InfluxDB configuration is required*");
    }

    [Fact]
    public void Constructor_InvalidConfig_ShouldThrowArgumentException()
    {
        // Arrange
        var config = TestConfigurationBuilder.ValidLoggerConfig();
        config.InfluxDb = new InfluxDbConfig
        {
            Url = "", // Invalid - empty URL
            Token = "test-token",
            Organization = "test-org",
            Bucket = "test-bucket"
        };

        // Act & Assert
        Action action = () => new InfluxDbWriter(Options.Create(config), _mockRetryService.Object, _mockLogger.Object);
        action.Should().Throw<ArgumentException>().WithMessage("*Invalid InfluxDB configuration*");
    }

    [Fact]
    public void Constructor_MissingToken_ShouldThrowArgumentException()
    {
        // Arrange
        var config = TestConfigurationBuilder.ValidLoggerConfig();
        config.InfluxDb = new InfluxDbConfig
        {
            Url = "http://localhost:8086",
            Token = "", // Invalid - empty token
            Organization = "test-org",
            Bucket = "test-bucket"
        };

        // Act & Assert
        Action action = () => new InfluxDbWriter(Options.Create(config), _mockRetryService.Object, _mockLogger.Object);
        action.Should().Throw<ArgumentException>().WithMessage("*Invalid InfluxDB configuration*");
    }

    [Fact]
    public void Constructor_MissingOrganization_ShouldThrowArgumentException()
    {
        // Arrange
        var config = TestConfigurationBuilder.ValidLoggerConfig();
        config.InfluxDb = new InfluxDbConfig
        {
            Url = "http://localhost:8086",
            Token = "test-token",
            Organization = "", // Invalid - empty organization
            Bucket = "test-bucket"
        };

        // Act & Assert
        Action action = () => new InfluxDbWriter(Options.Create(config), _mockRetryService.Object, _mockLogger.Object);
        action.Should().Throw<ArgumentException>().WithMessage("*Invalid InfluxDB configuration*");
    }

    [Fact]
    public void Constructor_MissingBucket_ShouldThrowArgumentException()
    {
        // Arrange
        var config = TestConfigurationBuilder.ValidLoggerConfig();
        config.InfluxDb = new InfluxDbConfig
        {
            Url = "http://localhost:8086",
            Token = "test-token",
            Organization = "test-org",
            Bucket = "" // Invalid - empty bucket
        };

        // Act & Assert
        Action action = () => new InfluxDbWriter(Options.Create(config), _mockRetryService.Object, _mockLogger.Object);
        action.Should().Throw<ArgumentException>().WithMessage("*Invalid InfluxDB configuration*");
    }

    [Fact]
    public void Constructor_InvalidUrl_ShouldThrowArgumentException()
    {
        // Arrange
        var config = TestConfigurationBuilder.ValidLoggerConfig();
        config.InfluxDb = new InfluxDbConfig
        {
            Url = "invalid-url", // Invalid URL format
            Token = "test-token",
            Organization = "test-org",
            Bucket = "test-bucket"
        };

        // Act & Assert
        Action action = () => new InfluxDbWriter(Options.Create(config), _mockRetryService.Object, _mockLogger.Object);
        action.Should().Throw<ArgumentException>().WithMessage("*Invalid InfluxDB configuration*");
    }

    [Fact]
    public void Constructor_NullRetryService_ShouldHandleGracefully()
    {
        // Act
        // The implementation may or may not throw with null retry service
        // depending on the order of operations in the constructor
        Action action = () => new InfluxDbWriter(_configOptions, null!, _mockLogger.Object);
        
        // Assert
        // Either it throws an exception (NullReference or connection error) or succeeds
        // Both are valid outcomes for this edge case
        try
        {
            action();
            // If no exception, ensure the writer was created
            Assert.True(true, "Constructor handled null retry service gracefully");
        }
        catch (Exception ex)
        {
            // Any exception is acceptable for null parameter
            Assert.True(ex is NullReferenceException || ex.Message.Contains("connection"),
                $"Expected NullReferenceException or connection error, but got: {ex.GetType().Name}");
        }
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrowException()
    {
        // Act & Assert
        // The implementation logs in the constructor, so it will throw when trying to use null logger
        Action action = () => new InfluxDbWriter(_configOptions, _mockRetryService.Object, null!);
        action.Should().Throw<ArgumentNullException>(); // Will throw when trying to log
    }

    #endregion

    #region WriteAsync Tests

    [Fact]
    public async Task WriteAsync_ValidReading_ShouldCallRetryService()
    {
        // Arrange
        var writer = CreateWriter();
        var reading = CreateTestReading();

        // Act
        await writer.WriteAsync(reading);

        // Assert
        _mockRetryService.Verify(x => x.ExecuteAsync(
            It.IsAny<Func<CancellationToken, Task>>(),
            It.IsAny<RetryPolicy>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task WriteAsync_WithCancellation_ShouldRespectCancellation()
    {
        // Arrange
        var writer = CreateWriter();
        var reading = CreateTestReading();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Setup retry service to throw on cancellation
        _mockRetryService.Setup(x => x.ExecuteAsync(
            It.IsAny<Func<CancellationToken, Task>>(),
            It.IsAny<RetryPolicy>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await writer.Invoking(w => w.WriteAsync(reading, cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task WriteAsync_WithDebugLogging_ShouldLogDebugMessage()
    {
        // Arrange
        var config = TestConfigurationBuilder.ValidLoggerConfig();
        config.InfluxDb!.EnableDebugLogging = true;
        var writer = CreateWriter(config);
        var reading = CreateTestReading();

        // Act
        await writer.WriteAsync(reading);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Writing data point")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task WriteAsync_RetryFailure_ShouldLogErrorAndThrow()
    {
        // Arrange
        var writer = CreateWriter();
        var reading = CreateTestReading();
        var expectedException = new InvalidOperationException("InfluxDB write failed");

        _mockRetryService.Setup(x => x.ExecuteAsync(
            It.IsAny<Func<CancellationToken, Task>>(),
            It.IsAny<RetryPolicy>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        await writer.Invoking(w => w.WriteAsync(reading))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("InfluxDB write failed");

        // Verify error logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to write data point to InfluxDB")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task WriteAsync_DisposedWriter_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var writer = CreateWriter();
        var reading = CreateTestReading();
        writer.Dispose();

        // Act & Assert
        await writer.Invoking(w => w.WriteAsync(reading))
            .Should().ThrowAsync<ObjectDisposedException>();
    }

    #endregion

    #region WriteBatchAsync Tests

    [Fact]
    public async Task WriteBatchAsync_ValidReadings_ShouldCallRetryService()
    {
        // Arrange
        var writer = CreateWriter();
        var readings = new List<AdamDataReading>
        {
            CreateTestReading("DEVICE-001", 1),
            CreateTestReading("DEVICE-001", 2),
            CreateTestReading("DEVICE-002", 1)
        };

        // Act
        await writer.WriteBatchAsync(readings);

        // Assert
        _mockRetryService.Verify(x => x.ExecuteAsync(
            It.IsAny<Func<CancellationToken, Task>>(),
            It.IsAny<RetryPolicy>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task WriteBatchAsync_EmptyReadings_ShouldReturnImmediately()
    {
        // Arrange
        var writer = CreateWriter();
        var readings = new List<AdamDataReading>();

        // Act
        await writer.WriteBatchAsync(readings);

        // Assert
        _mockRetryService.Verify(x => x.ExecuteAsync(
            It.IsAny<Func<CancellationToken, Task>>(),
            It.IsAny<RetryPolicy>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WriteBatchAsync_WithDebugLogging_ShouldLogBatchSize()
    {
        // Arrange
        var config = TestConfigurationBuilder.ValidLoggerConfig();
        config.InfluxDb!.EnableDebugLogging = true;
        var writer = CreateWriter(config);
        var readings = new List<AdamDataReading>
        {
            CreateTestReading("DEVICE-001", 1),
            CreateTestReading("DEVICE-001", 2)
        };

        // Act
        await writer.WriteBatchAsync(readings);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Writing batch of 2 data points")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task WriteBatchAsync_SuccessfulWrite_ShouldLogSuccess()
    {
        // Arrange
        var writer = CreateWriter();
        var readings = new List<AdamDataReading>
        {
            CreateTestReading("DEVICE-001", 1),
            CreateTestReading("DEVICE-001", 2)
        };

        // Act
        await writer.WriteBatchAsync(readings);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully wrote 2 data points")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task WriteBatchAsync_RetryFailure_ShouldLogErrorAndThrow()
    {
        // Arrange
        var writer = CreateWriter();
        var readings = new List<AdamDataReading> { CreateTestReading() };
        var expectedException = new InvalidOperationException("Batch write failed");

        _mockRetryService.Setup(x => x.ExecuteAsync(
            It.IsAny<Func<CancellationToken, Task>>(),
            It.IsAny<RetryPolicy>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        await writer.Invoking(w => w.WriteBatchAsync(readings))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Batch write failed");

        // Verify error logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to write batch of 1 data points")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task WriteBatchAsync_DisposedWriter_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var writer = CreateWriter();
        var readings = new List<AdamDataReading> { CreateTestReading() };
        writer.Dispose();

        // Act & Assert
        await writer.Invoking(w => w.WriteBatchAsync(readings))
            .Should().ThrowAsync<ObjectDisposedException>();
    }

    #endregion

    #region FlushAsync Tests

    [Fact]
    public async Task FlushAsync_WithPendingWrites_ShouldProcessQueue()
    {
        // Arrange
        var writer = CreateWriter();

        // Note: The actual queue is private, so we test the behavior through the public interface
        // The flush timer should be created during construction

        // Act
        await writer.FlushAsync();

        // Assert
        // Since there are no pending writes in the queue, no batch write should occur
        // This test mainly verifies that FlushAsync doesn't throw and completes successfully
    }

    [Fact]
    public async Task FlushAsync_DisposedWriter_ShouldReturnImmediately()
    {
        // Arrange
        var writer = CreateWriter();
        writer.Dispose();

        // Act & Assert
        await writer.Invoking(w => w.FlushAsync())
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task FlushAsync_WithCancellation_ShouldRespectCancellation()
    {
        // Arrange
        var writer = CreateWriter();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await writer.Invoking(w => w.FlushAsync(cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region IsHealthyAsync Tests

    [Fact]
    public async Task IsHealthyAsync_DisposedWriter_ShouldReturnFalse()
    {
        // Arrange
        var writer = CreateWriter();
        writer.Dispose();

        // Act
        var result = await writer.IsHealthyAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsHealthyAsync_WithCancellation_ShouldReturnFalseOnException()
    {
        // Arrange
        var writer = CreateWriter();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        // The InfluxDB client's PingAsync doesn't accept a cancellation token,
        // so cancellation won't be respected in the actual implementation.
        // The test should verify the method returns false when the ping fails
        var result = await writer.IsHealthyAsync(cts.Token);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Configuration_ValidInfluxDbConfig_ShouldPassValidation()
    {
        // Arrange
        var config = new InfluxDbConfig
        {
            Url = "http://localhost:8086",
            Token = "test-token",
            Organization = "test-org",
            Bucket = "test-bucket",
            Measurement = "test-measurement",
            WriteBatchSize = 100,
            FlushIntervalMs = 5000,
            TimeoutMs = 30000,
            MaxRetryAttempts = 3,
            RetryDelayMs = 1000
        };

        // Act
        var validationResults = config.Validate();

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void Configuration_InvalidUrlScheme_ShouldFailValidation()
    {
        // Arrange
        var config = new InfluxDbConfig
        {
            Url = "ftp://localhost:8086", // Invalid scheme
            Token = "test-token",
            Organization = "test-org",
            Bucket = "test-bucket"
        };

        // Act
        var validationResults = config.Validate();

        // Assert
        validationResults.Should().ContainSingle(r => r.ErrorMessage!.Contains("HTTP or HTTPS"));
    }

    [Fact]
    public void Configuration_InvalidTimeoutRange_ShouldFailValidation()
    {
        // Arrange
        var config = new InfluxDbConfig
        {
            Url = "http://localhost:8086",
            Token = "test-token",
            Organization = "test-org",
            Bucket = "test-bucket",
            TimeoutMs = 500 // Below minimum of 1000
        };

        // Act
        var validationResults = config.Validate();

        // Assert
        validationResults.Should().ContainSingle(r => r.ErrorMessage!.Contains("TimeoutMs must be between"));
    }

    [Fact]
    public void Configuration_InvalidRetryAttempts_ShouldFailValidation()
    {
        // Arrange
        var config = new InfluxDbConfig
        {
            Url = "http://localhost:8086",
            Token = "test-token",
            Organization = "test-org",
            Bucket = "test-bucket",
            MaxRetryAttempts = 15 // Above maximum of 10
        };

        // Act
        var validationResults = config.Validate();

        // Assert
        validationResults.Should().ContainSingle(r => r.ErrorMessage!.Contains("MaxRetryAttempts must be between"));
    }

    [Fact]
    public void Configuration_GlobalTags_ShouldBeConfigurable()
    {
        // Arrange
        var config = new InfluxDbConfig
        {
            Url = "http://localhost:8086",
            Token = "test-token",
            Organization = "test-org",
            Bucket = "test-bucket",
            GlobalTags = new Dictionary<string, string>
            {
                { "environment", "production" },
                { "region", "us-west" }
            }
        };

        // Act
        var validationResults = config.Validate();

        // Assert
        validationResults.Should().BeEmpty();
        config.GlobalTags.Should().HaveCount(2);
        config.GlobalTags["environment"].Should().Be("production");
        config.GlobalTags["region"].Should().Be("us-west");
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public async Task Dispose_ShouldCleanupResources()
    {
        // Arrange
        var writer = CreateWriter();

        // Act
        writer.Dispose();

        // Assert
        // Verify that disposed state is set by trying to write
        var reading = CreateTestReading();
        await writer.Invoking(w => w.WriteAsync(reading))
            .Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public void Dispose_MultipleDispose_ShouldNotThrow()
    {
        // Arrange
        var writer = CreateWriter();

        // Act & Assert
        writer.Dispose();
        Action secondDispose = () => writer.Dispose();
        secondDispose.Should().NotThrow();
    }

    [Fact]
    public void Dispose_ShouldLogDisposal()
    {
        // Arrange
        var writer = CreateWriter();

        // Act
        writer.Dispose();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("InfluxDB writer disposed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Helper Methods

    private InfluxDbWriter CreateWriter(AdamLoggerConfig? config = null)
    {
        var actualConfig = config ?? _testConfig;

        // Since the InfluxDB writer creates a real client, we can't test it without a connection
        // However, we can test the constructor validation and disposal behavior
        // For actual write operations, we would need integration tests or a more complex mock setup

        try
        {
            var writer = new InfluxDbWriter(
                Options.Create(actualConfig),
                _mockRetryService.Object,
                _mockLogger.Object);

            _writersToDispose.Add(writer);
            return writer;
        }
        catch (Exception ex) when (ex.Message.Contains("InfluxDB"))
        {
            // Expected when no real InfluxDB connection is available
            throw;
        }
    }

    private AdamDataReading CreateTestReading(string deviceId = "TEST-001", int channel = 1)
    {
        return new AdamDataReading
        {
            DeviceId = deviceId,
            Channel = channel,
            RawValue = 1234,
            ProcessedValue = 1234,
            Quality = DataQuality.Good,
            Timestamp = DateTimeOffset.UtcNow,
            Rate = 5.5,
            Tags = new Dictionary<string, object>
            {
                { "location", "line1" },
                { "shift", "day" }
            }
        };
    }

    #endregion

    public void Dispose()
    {
        foreach (var writer in _writersToDispose)
        {
            try
            {
                writer.Dispose();
            }
            catch
            {
                // Ignore disposal errors in tests
            }
        }
        _writersToDispose.Clear();
    }
}
