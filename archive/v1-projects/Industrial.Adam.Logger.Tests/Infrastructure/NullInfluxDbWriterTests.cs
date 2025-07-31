// Industrial.Adam.Logger.Tests - NullInfluxDbWriter Unit Tests
// Comprehensive tests for the null object pattern implementation

using FluentAssertions;
using Industrial.Adam.Logger.Infrastructure;
using Industrial.Adam.Logger.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Industrial.Adam.Logger.Tests.Infrastructure;

public class NullInfluxDbWriterTests
{
    private readonly Mock<ILogger<NullInfluxDbWriter>> _mockLogger;
    private readonly NullInfluxDbWriter _nullWriter;

    public NullInfluxDbWriterTests()
    {
        _mockLogger = new Mock<ILogger<NullInfluxDbWriter>>();
        _nullWriter = new NullInfluxDbWriter(_mockLogger.Object);
    }

    [Fact]
    public void Constructor_ShouldLogInformationMessage()
    {
        // Arrange
        var freshMockLogger = new Mock<ILogger<NullInfluxDbWriter>>();
        
        // Act
        var writer = new NullInfluxDbWriter(freshMockLogger.Object);

        // Assert
        freshMockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("InfluxDB not configured, using null writer")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task WriteAsync_WithValidReading_ShouldCompleteSuccessfully()
    {
        // Arrange
        var reading = new AdamDataReading
        {
            DeviceId = "test-device",
            Channel = 0,
            RawValue = 2550,
            Timestamp = DateTimeOffset.UtcNow,
            ProcessedValue = 25.5,
            Quality = DataQuality.Good
        };

        // Act
        var task = _nullWriter.WriteAsync(reading);

        // Assert
        await task;
        task.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public async Task WriteAsync_WithNullReading_ShouldCompleteSuccessfully()
    {
        // Arrange
        AdamDataReading? nullReading = null;

        // Act
        var task = _nullWriter.WriteAsync(nullReading!);

        // Assert
        await task;
        task.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public async Task WriteAsync_WithCancellationToken_ShouldCompleteSuccessfully()
    {
        // Arrange
        var reading = new AdamDataReading
        {
            DeviceId = "test-device",
            Channel = 1,
            RawValue = 3020,
            Timestamp = DateTimeOffset.UtcNow,
            ProcessedValue = 30.2,
            Quality = DataQuality.Good
        };
        using var cts = new CancellationTokenSource();

        // Act
        var task = _nullWriter.WriteAsync(reading, cts.Token);

        // Assert
        await task;
        task.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public async Task WriteAsync_WithCancelledToken_ShouldNotThrow()
    {
        // Arrange
        var reading = new AdamDataReading
        {
            DeviceId = "test-device",
            Channel = 2,
            RawValue = 1500,
            Timestamp = DateTimeOffset.UtcNow,
            ProcessedValue = 15.0,
            Quality = DataQuality.Good
        };
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var task = _nullWriter.WriteAsync(reading, cts.Token);
        await task; // Should not throw
        task.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public async Task WriteBatchAsync_WithValidReadings_ShouldCompleteSuccessfully()
    {
        // Arrange
        var readings = new List<AdamDataReading>
        {
            new AdamDataReading
            {
                DeviceId = "device-1",
                Channel = 0,
                RawValue = 2550,
                Timestamp = DateTimeOffset.UtcNow,
                ProcessedValue = 25.5,
                Quality = DataQuality.Good
            },
            new AdamDataReading
            {
                DeviceId = "device-2",
                Channel = 1,
                RawValue = 3020,
                Timestamp = DateTimeOffset.UtcNow.AddSeconds(1),
                ProcessedValue = 30.2,
                Quality = DataQuality.Good
            }
        };

        // Act
        var task = _nullWriter.WriteBatchAsync(readings);

        // Assert
        await task;
        task.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public async Task WriteBatchAsync_WithEmptyReadings_ShouldCompleteSuccessfully()
    {
        // Arrange
        var emptyReadings = new List<AdamDataReading>();

        // Act
        var task = _nullWriter.WriteBatchAsync(emptyReadings);

        // Assert
        await task;
        task.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public async Task WriteBatchAsync_WithNullReadings_ShouldCompleteSuccessfully()
    {
        // Arrange
        IEnumerable<AdamDataReading>? nullReadings = null;

        // Act
        var task = _nullWriter.WriteBatchAsync(nullReadings!);

        // Assert
        await task;
        task.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public async Task WriteBatchAsync_WithCancellationToken_ShouldCompleteSuccessfully()
    {
        // Arrange
        var readings = new List<AdamDataReading>
        {
            new AdamDataReading
            {
                DeviceId = "test-device",
                Channel = 0,
                RawValue = 4200,
                Timestamp = DateTimeOffset.UtcNow,
                ProcessedValue = 42.0,
                Quality = DataQuality.Good
            }
        };
        using var cts = new CancellationTokenSource();

        // Act
        var task = _nullWriter.WriteBatchAsync(readings, cts.Token);

        // Assert
        await task;
        task.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public async Task WriteBatchAsync_WithCancelledToken_ShouldNotThrow()
    {
        // Arrange
        var readings = new List<AdamDataReading>
        {
            new AdamDataReading
            {
                DeviceId = "test-device",
                Channel = 0,
                RawValue = 3500,
                Timestamp = DateTimeOffset.UtcNow,
                ProcessedValue = 35.0,
                Quality = DataQuality.Good
            }
        };
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var task = _nullWriter.WriteBatchAsync(readings, cts.Token);
        await task; // Should not throw
        task.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public async Task FlushAsync_ShouldCompleteSuccessfully()
    {
        // Act
        var task = _nullWriter.FlushAsync();

        // Assert
        await task;
        task.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public async Task FlushAsync_WithCancellationToken_ShouldCompleteSuccessfully()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        var task = _nullWriter.FlushAsync(cts.Token);

        // Assert
        await task;
        task.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public async Task FlushAsync_WithCancelledToken_ShouldNotThrow()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var task = _nullWriter.FlushAsync(cts.Token);
        await task; // Should not throw
        task.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public async Task IsHealthyAsync_ShouldAlwaysReturnTrue()
    {
        // Act
        var result = await _nullWriter.IsHealthyAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsHealthyAsync_WithCancellationToken_ShouldReturnTrue()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        var result = await _nullWriter.IsHealthyAsync(cts.Token);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsHealthyAsync_WithCancelledToken_ShouldReturnTrue()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await _nullWriter.IsHealthyAsync(cts.Token);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Dispose_ShouldCompleteSuccessfully()
    {
        // Act & Assert
        _nullWriter.Dispose(); // Should not throw
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_ShouldNotThrow()
    {
        // Act & Assert
        _nullWriter.Dispose(); // Should not throw
        _nullWriter.Dispose(); // Should not throw (idempotent)
        _nullWriter.Dispose(); // Should not throw (idempotent)
    }

    [Fact]
    public async Task Operations_AfterDispose_ShouldStillWork()
    {
        // Arrange
        var reading = new AdamDataReading
        {
            DeviceId = "test-device",
            Channel = 0,
            RawValue = 1000,
            Timestamp = DateTimeOffset.UtcNow,
            ProcessedValue = 10.0,
            Quality = DataQuality.Good
        };

        // Act
        _nullWriter.Dispose();

        // Assert - All operations should still work (null object pattern)
        await _nullWriter.WriteAsync(reading);
        await _nullWriter.WriteBatchAsync(new[] { reading });
        await _nullWriter.FlushAsync();
        var healthy = await _nullWriter.IsHealthyAsync();
        healthy.Should().BeTrue();
    }

    [Fact]
    public async Task ConcurrentOperations_ShouldAllCompleteSuccessfully()
    {
        // Arrange
        var reading = new AdamDataReading
        {
            DeviceId = "test-device",
            Channel = 0,
            RawValue = 4200,
            Timestamp = DateTimeOffset.UtcNow,
            ProcessedValue = 42.0,
            Quality = DataQuality.Good
        };

        // Act
        var tasks = new List<Task>();
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(_nullWriter.WriteAsync(reading));
            tasks.Add(_nullWriter.WriteBatchAsync(new[] { reading }));
            tasks.Add(_nullWriter.FlushAsync());
            tasks.Add(_nullWriter.IsHealthyAsync());
        }

        // Assert
        await Task.WhenAll(tasks);
        tasks.All(t => t.IsCompletedSuccessfully).Should().BeTrue();
    }
}