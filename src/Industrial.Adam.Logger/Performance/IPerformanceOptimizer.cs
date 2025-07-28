// Industrial.Adam.Logger - Performance Optimization Interfaces
// Interfaces for optimizing performance in high-frequency counter applications

using Industrial.Adam.Logger.Configuration;
using Industrial.Adam.Logger.Models;

namespace Industrial.Adam.Logger.Performance;

/// <summary>
/// Interface for performance optimization services in industrial counter applications
/// Provides methods to optimize data processing, memory usage, and network communications
/// </summary>
public interface IPerformanceOptimizer
{
    /// <summary>
    /// Optimize data processing pipeline for high-frequency counter operations
    /// </summary>
    /// <param name="config">Current logger configuration</param>
    /// <returns>Task that completes when optimization is applied</returns>
    Task OptimizeDataProcessingAsync(AdamLoggerConfig config);

    /// <summary>
    /// Optimize memory usage for continuous operation scenarios
    /// </summary>
    /// <param name="expectedRuntime">Expected runtime duration in hours</param>
    /// <param name="dataPointsPerHour">Expected data points per hour</param>
    /// <returns>Task that completes when memory optimization is applied</returns>
    Task OptimizeMemoryUsageAsync(double expectedRuntime, int dataPointsPerHour);

    /// <summary>
    /// Optimize network communications for high-throughput scenarios
    /// </summary>
    /// <param name="deviceConfigs">List of device configurations to optimize</param>
    /// <returns>Task that completes when network optimization is applied</returns>
    Task OptimizeNetworkCommunicationsAsync(IEnumerable<AdamDeviceConfig> deviceConfigs);

    /// <summary>
    /// Get current performance metrics and optimization recommendations
    /// </summary>
    /// <returns>Performance metrics with optimization suggestions</returns>
    Task<PerformanceMetrics> GetPerformanceMetricsAsync();
}

/// <summary>
/// Interface for counter-specific data processing optimizations
/// Focuses on high-frequency counter value processing and rate calculations
/// </summary>
public interface ICounterDataProcessor
{
    /// <summary>
    /// Process counter data with optimized algorithms for high-frequency operations
    /// </summary>
    /// <param name="readings">Batch of counter readings to process</param>
    /// <param name="batchTimeout">Maximum time to wait for batch completion</param>
    /// <returns>Processed readings with performance metrics</returns>
    Task<CounterProcessingResult> ProcessCounterBatchAsync(
        IEnumerable<AdamDataReading> readings,
        TimeSpan batchTimeout);

    /// <summary>
    /// Calculate rates for multiple counters efficiently using vectorized operations
    /// </summary>
    /// <param name="counterData">Dictionary of device/channel to value history</param>
    /// <returns>Dictionary of calculated rates with performance timing</returns>
    Task<Dictionary<string, double?>> CalculateRatesBatchAsync(
        Dictionary<string, List<(DateTimeOffset timestamp, long value)>> counterData);

    /// <summary>
    /// Optimize rate calculation window size based on data frequency and accuracy requirements
    /// </summary>
    /// <param name="dataFrequency">Data collection frequency in Hz</param>
    /// <param name="accuracyRequirement">Required accuracy as a percentage</param>
    /// <returns>Optimal window size in seconds</returns>
    TimeSpan OptimizeRateCalculationWindow(double dataFrequency, double accuracyRequirement);
}

/// <summary>
/// Interface for memory management in continuous operation scenarios
/// Optimizes memory usage for 24/7 industrial applications
/// </summary>
public interface IMemoryManager
{
    /// <summary>
    /// Configure memory pools for efficient allocation and reuse
    /// </summary>
    /// <param name="expectedConcurrentOperations">Expected concurrent operations</param>
    /// <param name="averageObjectSize">Average size of objects in bytes</param>
    /// <returns>Task that completes when memory pools are configured</returns>
    Task ConfigureMemoryPoolsAsync(int expectedConcurrentOperations, int averageObjectSize);

    /// <summary>
    /// Implement efficient garbage collection strategies for continuous operation
    /// </summary>
    /// <param name="targetMemoryUsageMB">Target memory usage in megabytes</param>
    /// <returns>Task that completes when GC strategy is applied</returns>
    Task OptimizeGarbageCollectionAsync(int targetMemoryUsageMB);

    /// <summary>
    /// Clean up historical data to prevent memory leaks
    /// </summary>
    /// <param name="retentionPeriod">How long to retain data in memory</param>
    /// <returns>Task that completes when cleanup is finished</returns>
    Task CleanupHistoricalDataAsync(TimeSpan retentionPeriod);

    /// <summary>
    /// Get current memory usage statistics
    /// </summary>
    /// <returns>Memory usage metrics and recommendations</returns>
    MemoryMetrics GetMemoryMetrics();
}

/// <summary>
/// Interface for network communication optimization
/// Optimizes Modbus TCP communications for high-throughput scenarios
/// </summary>
public interface INetworkOptimizer
{
    /// <summary>
    /// Optimize connection pooling for multiple devices
    /// </summary>
    /// <param name="deviceConfigs">List of device configurations</param>
    /// <returns>Task that completes when connection pooling is optimized</returns>
    Task OptimizeConnectionPoolingAsync(IEnumerable<AdamDeviceConfig> deviceConfigs);

    /// <summary>
    /// Configure optimal batch sizes for register reads
    /// </summary>
    /// <param name="networkLatency">Average network latency in milliseconds</param>
    /// <param name="deviceResponseTime">Average device response time in milliseconds</param>
    /// <returns>Optimal batch size for register reads</returns>
    int CalculateOptimalBatchSize(double networkLatency, double deviceResponseTime);

    /// <summary>
    /// Implement request pipelining to reduce network round trips
    /// </summary>
    /// <param name="maxConcurrentRequests">Maximum concurrent requests per device</param>
    /// <returns>Task that completes when pipelining is configured</returns>
    Task ConfigureRequestPipeliningAsync(int maxConcurrentRequests);

    /// <summary>
    /// Get network performance metrics
    /// </summary>
    /// <returns>Network performance statistics and optimization suggestions</returns>
    NetworkMetrics GetNetworkMetrics();
}

/// <summary>
/// Performance metrics for the overall system
/// </summary>
public record PerformanceMetrics
{
    public double CpuUsagePercent { get; init; }
    public long MemoryUsageMB { get; init; }
    public double DataProcessingRatePerSecond { get; init; }
    public double AverageResponseTimeMs { get; init; }
    public int ActiveConnections { get; init; }
    public int QueuedOperations { get; init; }
    public List<string> OptimizationRecommendations { get; init; } = new();
    public DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Memory usage metrics and recommendations
/// </summary>
public record MemoryMetrics
{
    public long TotalMemoryMB { get; init; }
    public long UsedMemoryMB { get; init; }
    public long AvailableMemoryMB { get; init; }
    public double MemoryUsagePercent { get; init; }
    public int GarbageCollectionCount { get; init; }
    public double AverageGCTimeMs { get; init; }
    public List<string> MemoryOptimizationSuggestions { get; init; } = new();
    public DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Network performance metrics and optimization suggestions
/// </summary>
public record NetworkMetrics
{
    public double AverageLatencyMs { get; init; }
    public double ThroughputMbps { get; init; }
    public int ActiveConnections { get; init; }
    public int FailedConnections { get; init; }
    public double PacketLossPercent { get; init; }
    public List<string> NetworkOptimizationSuggestions { get; init; } = new();
    public DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Result of batch counter processing operations
/// </summary>
public record CounterProcessingResult
{
    public IReadOnlyList<AdamDataReading> ProcessedReadings { get; init; } = new List<AdamDataReading>();
    public int SuccessfulProcessingCount { get; init; }
    public int FailedProcessingCount { get; init; }
    public TimeSpan ProcessingDuration { get; init; }
    public double ProcessingRatePerSecond { get; init; }
    public List<string> ProcessingErrors { get; init; } = new();
    public DateTimeOffset Timestamp { get; init; }
}
