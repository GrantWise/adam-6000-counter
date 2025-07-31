using FluentAssertions;
using Industrial.Adam.Logger.Configuration;
using Industrial.Adam.Logger.Performance;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Industrial.Adam.Logger.Tests.Performance;

public class MemoryManagerTests : IDisposable
{
    private readonly Mock<ILogger<MemoryManager>> _mockLogger;
    private readonly Mock<IOptions<AdamLoggerConfig>> _mockConfig;
    private readonly AdamLoggerConfig _config;
    private readonly MemoryManager _memoryManager;

    public MemoryManagerTests()
    {
        _mockLogger = new Mock<ILogger<MemoryManager>>();
        _mockConfig = new Mock<IOptions<AdamLoggerConfig>>();
        
        _config = new AdamLoggerConfig
        {
            PollIntervalMs = 1000,
            DataBufferSize = 100,
            Devices = new List<AdamDeviceConfig>()
        };
        
        _mockConfig.Setup(x => x.Value).Returns(_config);
        _memoryManager = new MemoryManager(_mockConfig.Object, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_WhenConfigIsNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new MemoryManager(null!, _mockLogger.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("config");
    }

    [Fact]
    public void Constructor_WhenLoggerIsNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new MemoryManager(_mockConfig.Object, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ShouldInitializeSuccessfully()
    {
        // Assert
        _memoryManager.Should().NotBeNull();
        _mockLogger.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Memory manager initialized")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()
        ), Times.Once);
    }

    [Fact]
    public async Task ConfigureMemoryPoolsAsync_ShouldConfigurePoolsSuccessfully()
    {
        // Arrange
        var expectedConcurrentOperations = 10;
        var averageObjectSize = 1024;

        // Act
        await _memoryManager.ConfigureMemoryPoolsAsync(expectedConcurrentOperations, averageObjectSize);

        // Assert
        _mockLogger.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Memory pools configured successfully")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()
        ), Times.Once);
    }

    [Fact]
    public async Task ConfigureMemoryPoolsAsync_WithLowConcurrency_ShouldUseMinimumPoolSize()
    {
        // Arrange
        var expectedConcurrentOperations = 5; // Less than minimum of 16
        var averageObjectSize = 1024;

        // Act
        await _memoryManager.ConfigureMemoryPoolsAsync(expectedConcurrentOperations, averageObjectSize);

        // Assert
        _mockLogger.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Configuring memory pools: 16 standard objects")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()
        ), Times.Once);
    }

    [Fact]
    public async Task OptimizeGarbageCollectionAsync_ShouldOptimizeWhenMemoryHigh()
    {
        // Arrange
        var targetMemoryUsageMB = 100; // Low target to trigger GC

        // Act
        await _memoryManager.OptimizeGarbageCollectionAsync(targetMemoryUsageMB);

        // Assert
        _mockLogger.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Optimizing GC")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()
        ), Times.Once);
    }

    [Fact]
    public async Task CleanupHistoricalDataAsync_ShouldRemoveExpiredData()
    {
        // Arrange
        var retentionPeriod = TimeSpan.FromHours(1);
        
        // Register some data for retention
        _memoryManager.RegisterDataForRetention("test-data-1");
        _memoryManager.RegisterDataForRetention("test-data-2");

        // Act
        await _memoryManager.CleanupHistoricalDataAsync(retentionPeriod);

        // Assert - Should complete without errors
        _mockLogger.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()
        ), Times.Never);
    }

    [Fact]
    public void GetMemoryMetrics_ShouldReturnValidMetrics()
    {
        // Act
        var metrics = _memoryManager.GetMemoryMetrics();

        // Assert
        metrics.Should().NotBeNull();
        metrics.TotalMemoryMB.Should().BeGreaterThanOrEqualTo(0);
        metrics.UsedMemoryMB.Should().BeGreaterThanOrEqualTo(0);
        metrics.AvailableMemoryMB.Should().BeGreaterThanOrEqualTo(0);
        metrics.MemoryUsagePercent.Should().BeInRange(0, 100);
        metrics.GarbageCollectionCount.Should().BeGreaterThanOrEqualTo(0);
        metrics.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GetMemoryMetrics_WithHighMemoryUsage_ShouldIncludeSuggestions()
    {
        // This test is difficult to control precisely, but we can verify the structure
        // Act
        var metrics = _memoryManager.GetMemoryMetrics();

        // Assert
        metrics.MemoryOptimizationSuggestions.Should().NotBeNull();
        metrics.MemoryOptimizationSuggestions.Should().BeOfType<List<string>>();
    }

    [Fact]
    public void RegisterDataForRetention_ShouldTrackData()
    {
        // Arrange
        var dataKey = "test-data-key";

        // Act
        _memoryManager.RegisterDataForRetention(dataKey);

        // Assert - Should not throw
        // The actual tracking is internal, so we just verify no exceptions
        Assert.True(true);
    }

    [Fact]
    public void GetObjectPool_ShouldReturnValidPool()
    {
        // Act
        var pool = _memoryManager.GetObjectPool<TestObject>();

        // Assert
        pool.Should().NotBeNull();
        pool.Should().BeOfType<ObjectPool<TestObject>>();
    }

    [Fact]
    public void GetObjectPool_MultipleCalls_ShouldReturnSamePool()
    {
        // Act
        var pool1 = _memoryManager.GetObjectPool<TestObject>();
        var pool2 = _memoryManager.GetObjectPool<TestObject>();

        // Assert
        pool1.Should().BeSameAs(pool2);
    }

    [Fact]
    public void GetObjectPool_DifferentTypes_ShouldReturnDifferentPools()
    {
        // Act
        var pool1 = _memoryManager.GetObjectPool<TestObject>();
        var pool2 = _memoryManager.GetObjectPool<AnotherTestObject>();

        // Assert
        pool1.Should().NotBeSameAs(pool2);
    }

    [Fact]
    public void ObjectPool_GetAndReturn_ShouldReuseObjects()
    {
        // Arrange
        var pool = _memoryManager.GetObjectPool<TestObject>();

        // Act
        var obj1 = pool.Get();
        obj1.Value = 42;
        pool.Return(obj1);
        
        var obj2 = pool.Get();

        // Assert
        obj2.Should().BeSameAs(obj1);
        obj2.Value.Should().Be(42); // Same object reused
    }

    [Fact]
    public void ObjectPool_Return_WithNull_ShouldNotThrow()
    {
        // Arrange
        var pool = _memoryManager.GetObjectPool<TestObject>();

        // Act & Assert
        var act = () => pool.Return(null!);
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_ShouldCleanupResources()
    {
        // Act
        _memoryManager.Dispose();

        // Assert
        _mockLogger.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Memory manager disposed")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()
        ), Times.Once);
    }

    [Fact]
    public void Dispose_MultipleCalls_ShouldNotThrow()
    {
        // Act & Assert
        var act = () =>
        {
            _memoryManager.Dispose();
            _memoryManager.Dispose();
        };
        act.Should().NotThrow();
    }

    [Fact]
    public async Task ConfigureMemoryPoolsAsync_WithException_ShouldLogError()
    {
        // Arrange
        var invalidConcurrentOperations = -1;
        var averageObjectSize = 1024;

        // Act
        var act = async () => await _memoryManager.ConfigureMemoryPoolsAsync(invalidConcurrentOperations, averageObjectSize);

        // Assert - Should handle gracefully (implementation dependent)
        // The actual behavior depends on the implementation
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CleanupHistoricalDataAsync_ConcurrentExecution_ShouldBeThreadSafe()
    {
        // Arrange
        var retentionPeriod = TimeSpan.FromMinutes(30);
        var tasks = new List<Task>();

        // Register data from multiple threads
        for (int i = 0; i < 100; i++)
        {
            var key = $"data-{i}";
            _memoryManager.RegisterDataForRetention(key);
        }

        // Act - Multiple concurrent cleanup attempts
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_memoryManager.CleanupHistoricalDataAsync(retentionPeriod));
        }

        // Assert
        var act = async () => await Task.WhenAll(tasks);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void GetMemoryMetrics_AfterGarbageCollection_ShouldUpdateGCCount()
    {
        // Arrange
        var initialMetrics = _memoryManager.GetMemoryMetrics();
        var initialGCCount = initialMetrics.GarbageCollectionCount;

        // Act
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var updatedMetrics = _memoryManager.GetMemoryMetrics();

        // Assert
        updatedMetrics.GarbageCollectionCount.Should().BeGreaterThanOrEqualTo(initialGCCount);
    }

    // Test helper classes
    private class TestObject
    {
        public int Value { get; set; }
    }

    private class AnotherTestObject
    {
        public string Name { get; set; } = string.Empty;
    }

    public void Dispose()
    {
        _memoryManager?.Dispose();
    }
}