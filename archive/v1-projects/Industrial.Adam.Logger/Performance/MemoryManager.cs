// Industrial.Adam.Logger - Memory Management Implementation
// Optimized memory management for continuous 24/7 industrial operations

using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime;
using Industrial.Adam.Logger.Configuration;
using Industrial.Adam.Logger.Performance;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Industrial.Adam.Logger.Performance;

/// <summary>
/// Memory manager optimized for continuous industrial operations
/// Implements memory pooling, garbage collection optimization, and automatic cleanup
/// </summary>
public class MemoryManager : IMemoryManager, IDisposable
{
    private readonly ILogger<MemoryManager> _logger;
    private readonly AdamLoggerConfig _config;

    // Memory pools for common object types
    private readonly ConcurrentDictionary<Type, object> _objectPools = new();
    private readonly ArrayPool<byte> _byteArrayPool = ArrayPool<byte>.Shared;
    private readonly ArrayPool<double> _doubleArrayPool = ArrayPool<double>.Shared;
    private readonly ArrayPool<long> _longArrayPool = ArrayPool<long>.Shared;

    // Memory tracking
    private readonly object _metricsLock = new();
    private long _totalAllocatedBytes;
    private long _totalReleasedBytes;
    private int _gcCollectionCount;
    private double _averageGcTimeMs;
    private readonly List<TimeSpan> _gcTimes = new();

    // Cleanup management
    private readonly Timer _cleanupTimer;
    private readonly ConcurrentDictionary<string, DateTime> _dataRetentionTracker = new();
    private readonly TimeSpan _defaultRetentionPeriod = TimeSpan.FromHours(24);

    /// <summary>
    /// Initialize memory manager with optimized settings for industrial applications
    /// </summary>
    /// <param name="config">Logger configuration for memory optimization parameters</param>
    /// <param name="logger">Logger for diagnostic information</param>
    public MemoryManager(
        IOptions<AdamLoggerConfig> config,
        ILogger<MemoryManager> logger)
    {
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Configure garbage collection for industrial scenarios
        ConfigureGarbageCollection();

        // Set up periodic cleanup (every 30 minutes)
        _cleanupTimer = new Timer(PerformPeriodicCleanup, null,
            TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30));

        _logger.LogInformation("Memory manager initialized for continuous operation with {BufferSize} data buffer",
            _config.DataBufferSize);
    }

    /// <summary>
    /// Configure memory pools for efficient allocation and reuse
    /// </summary>
    public async Task ConfigureMemoryPoolsAsync(int expectedConcurrentOperations, int averageObjectSize)
    {
        await Task.Run(() =>
        {
            try
            {
                // Calculate pool sizes based on expected usage
                var poolSize = Math.Max(16, expectedConcurrentOperations * 2);
                var largePoolSize = Math.Max(8, expectedConcurrentOperations);

                _logger.LogInformation("Configuring memory pools: {PoolSize} standard objects, {LargePoolSize} large objects",
                    poolSize, largePoolSize);

                // Pre-allocate memory pools to reduce allocation pressure
                PreAllocateMemoryPools(poolSize, averageObjectSize);

                // Configure array pools for common data types
                ConfigureArrayPools(expectedConcurrentOperations);

                _logger.LogInformation("Memory pools configured successfully for {ConcurrentOps} concurrent operations",
                    expectedConcurrentOperations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error configuring memory pools");
                throw;
            }
        });
    }

    /// <summary>
    /// Implement efficient garbage collection strategies for continuous operation
    /// </summary>
    public async Task OptimizeGarbageCollectionAsync(int targetMemoryUsageMB)
    {
        await Task.Run(() =>
        {
            try
            {
                var targetMemoryBytes = targetMemoryUsageMB * 1024 * 1024;
                var currentMemory = GC.GetTotalMemory(false);

                _logger.LogInformation("Optimizing GC: target {TargetMB}MB, current {CurrentMB}MB",
                    targetMemoryUsageMB, currentMemory / (1024 * 1024));

                // Configure GC settings for industrial applications
                if (GCSettings.IsServerGC)
                {
                    // Server GC is already optimized for throughput
                    _logger.LogDebug("Using Server GC mode for optimal throughput");
                }
                else
                {
                    _logger.LogWarning("Workstation GC detected - consider enabling Server GC for better performance");
                }

                // Set latency mode for continuous operation
                var originalMode = GCSettings.LatencyMode;
                if (originalMode != GCLatencyMode.SustainedLowLatency)
                {
                    GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
                    _logger.LogInformation("Set GC latency mode to SustainedLowLatency for continuous operation");
                }

                // Perform optimization GC if memory usage is high
                if (currentMemory > targetMemoryBytes)
                {
                    var stopwatch = Stopwatch.StartNew();

                    GC.Collect(2, GCCollectionMode.Optimized);
                    GC.WaitForPendingFinalizers();
                    GC.Collect(2, GCCollectionMode.Optimized);

                    stopwatch.Stop();

                    var newMemory = GC.GetTotalMemory(false);
                    var freedMemoryMB = (currentMemory - newMemory) / (1024 * 1024);

                    UpdateGcMetrics(stopwatch.Elapsed);

                    _logger.LogInformation("GC optimization completed: freed {FreedMB}MB in {ElapsedMs}ms",
                        freedMemoryMB, stopwatch.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during GC optimization");
                throw;
            }
        });
    }

    /// <summary>
    /// Clean up historical data to prevent memory leaks
    /// </summary>
    public async Task CleanupHistoricalDataAsync(TimeSpan retentionPeriod)
    {
        await Task.Run(() =>
        {
            try
            {
                var cutoffTime = DateTime.UtcNow - retentionPeriod;
                var itemsToRemove = new List<string>();

                // Find expired data entries
                foreach (var kvp in _dataRetentionTracker)
                {
                    if (kvp.Value < cutoffTime)
                    {
                        itemsToRemove.Add(kvp.Key);
                    }
                }

                // Remove expired entries
                var removedCount = 0;
                foreach (var key in itemsToRemove)
                {
                    if (_dataRetentionTracker.TryRemove(key, out _))
                    {
                        removedCount++;
                    }
                }

                if (removedCount > 0)
                {
                    _logger.LogDebug("Cleaned up {RemovedCount} expired data entries older than {RetentionHours}h",
                        removedCount, retentionPeriod.TotalHours);

                    // Trigger GC after cleanup to free memory
                    GC.Collect(1, GCCollectionMode.Optimized);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during historical data cleanup");
            }
        });
    }

    /// <summary>
    /// Get current memory usage statistics
    /// </summary>
    public MemoryMetrics GetMemoryMetrics()
    {
        lock (_metricsLock)
        {
            var totalMemory = GC.GetTotalMemory(false);
            var process = Process.GetCurrentProcess();
            var workingSet = process.WorkingSet64;

            // Calculate memory usage percentage (rough estimate)
            var availableMemory = Math.Max(0, workingSet - totalMemory);
            var memoryUsagePercent = totalMemory > 0 ? (double)totalMemory / workingSet * 100 : 0;

            // Calculate average GC time
            var avgGcTime = _gcTimes.Count > 0 ? _gcTimes.Average(t => t.TotalMilliseconds) : 0;

            var suggestions = GenerateMemoryOptimizationSuggestions(totalMemory, memoryUsagePercent);

            return new MemoryMetrics
            {
                TotalMemoryMB = totalMemory / (1024 * 1024),
                UsedMemoryMB = totalMemory / (1024 * 1024),
                AvailableMemoryMB = availableMemory / (1024 * 1024),
                MemoryUsagePercent = memoryUsagePercent,
                GarbageCollectionCount = _gcCollectionCount,
                AverageGCTimeMs = avgGcTime,
                MemoryOptimizationSuggestions = suggestions,
                Timestamp = DateTimeOffset.UtcNow
            };
        }
    }

    /// <summary>
    /// Register data for retention tracking
    /// </summary>
    public void RegisterDataForRetention(string dataKey)
    {
        _dataRetentionTracker[dataKey] = DateTime.UtcNow;
    }

    /// <summary>
    /// Get memory pool for specific object type
    /// </summary>
    public ObjectPool<T> GetObjectPool<T>() where T : class, new()
    {
        var poolKey = typeof(T);
        return (ObjectPool<T>)_objectPools.GetOrAdd(poolKey, _ => new ObjectPool<T>(() => new T()));
    }

    /// <summary>
    /// Pre-allocate memory pools to reduce runtime allocation pressure
    /// </summary>
    private void PreAllocateMemoryPools(int poolSize, int averageObjectSize)
    {
        try
        {
            // Pre-allocate byte arrays for common sizes
            var commonSizes = new[] { 256, 1024, 4096, 8192 };
            foreach (var size in commonSizes)
            {
                for (int i = 0; i < Math.Min(poolSize, 32); i++)
                {
                    var array = _byteArrayPool.Rent(size);
                    TrackMemoryAllocation(size);
                    _byteArrayPool.Return(array);
                    TrackMemoryRelease(size);
                }
            }

            _logger.LogDebug("Pre-allocated memory pools for common array sizes");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Warning during memory pool pre-allocation");
        }
    }

    /// <summary>
    /// Configure array pools for industrial data processing
    /// </summary>
    private void ConfigureArrayPools(int expectedConcurrentOperations)
    {
        // Array pools are already optimized by the runtime
        // Log configuration for monitoring
        _logger.LogDebug("Array pools configured for {ConcurrentOps} concurrent operations",
            expectedConcurrentOperations);
    }

    /// <summary>
    /// Configure garbage collection settings for industrial applications
    /// </summary>
    private void ConfigureGarbageCollection()
    {
        try
        {
            // Monitor GC notifications for metrics
            GC.RegisterForFullGCNotification(10, 10);

            _logger.LogInformation("GC configuration: IsServerGC={IsServerGC}, LatencyMode={LatencyMode}",
                GCSettings.IsServerGC, GCSettings.LatencyMode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error configuring garbage collection settings");
        }
    }

    /// <summary>
    /// Update garbage collection metrics
    /// </summary>
    private void UpdateGcMetrics(TimeSpan gcTime)
    {
        lock (_metricsLock)
        {
            _gcCollectionCount++;
            _gcTimes.Add(gcTime);

            // Keep only last 100 GC times for average calculation
            if (_gcTimes.Count > 100)
            {
                _gcTimes.RemoveRange(0, _gcTimes.Count - 100);
            }

            _averageGcTimeMs = _gcTimes.Average(t => t.TotalMilliseconds);
        }
    }

    /// <summary>
    /// Generate memory optimization suggestions based on current metrics
    /// </summary>
    private List<string> GenerateMemoryOptimizationSuggestions(long totalMemory, double memoryUsagePercent)
    {
        var suggestions = new List<string>();

        if (memoryUsagePercent > 80)
        {
            suggestions.Add("Memory usage is high (>80%). Consider increasing available memory or reducing data buffer size.");
        }

        if (_averageGcTimeMs > 100)
        {
            suggestions.Add($"Average GC time is high ({_averageGcTimeMs:F1}ms). Consider optimizing object allocation patterns.");
        }

        if (totalMemory > 1024 * 1024 * 1024) // 1GB
        {
            suggestions.Add("Memory usage exceeds 1GB. Consider implementing data archiving for long-running operations.");
        }

        if (_gcCollectionCount > 1000)
        {
            suggestions.Add("High GC frequency detected. Consider using object pooling for frequently allocated objects.");
        }

        return suggestions;
    }

    /// <summary>
    /// Periodic cleanup task
    /// </summary>
    private void PerformPeriodicCleanup(object? state)
    {
        try
        {
            _logger.LogDebug("Performing periodic memory cleanup");

            // Clean up expired data
            _ = Task.Run(() => CleanupHistoricalDataAsync(_defaultRetentionPeriod));

            // Optimize GC if needed
            var currentMemory = GC.GetTotalMemory(false);
            var memoryMB = currentMemory / (1024 * 1024);

            if (memoryMB > 500) // If using more than 500MB
            {
                _ = Task.Run(() => OptimizeGarbageCollectionAsync(400)); // Target 400MB
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during periodic cleanup");
        }
    }

    /// <summary>
    /// Track memory allocation for metrics
    /// </summary>
    private void TrackMemoryAllocation(long bytes)
    {
        lock (_metricsLock)
        {
            _totalAllocatedBytes += bytes;
        }
    }

    /// <summary>
    /// Track memory release for metrics
    /// </summary>
    private void TrackMemoryRelease(long bytes)
    {
        lock (_metricsLock)
        {
            _totalReleasedBytes += bytes;
        }
    }

    /// <summary>
    /// Dispose of memory manager resources
    /// </summary>
    public void Dispose()
    {
        try
        {
            _cleanupTimer?.Dispose();
            _objectPools.Clear();
            _dataRetentionTracker.Clear();

            _logger.LogInformation("Memory manager disposed. Final stats: {AllocatedMB}MB allocated, {GCCount} GC collections",
                _totalAllocatedBytes / (1024 * 1024), _gcCollectionCount);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during memory manager disposal");
        }
    }
}

/// <summary>
/// Simple object pool implementation for memory optimization
/// </summary>
public class ObjectPool<T> where T : class
{
    private readonly ConcurrentQueue<T> _objects = new();
    private readonly Func<T> _objectGenerator;
    private readonly int _maxSize;

    public ObjectPool(Func<T> objectGenerator, int maxSize = 100)
    {
        _objectGenerator = objectGenerator ?? throw new ArgumentNullException(nameof(objectGenerator));
        _maxSize = maxSize;
    }

    public T Get()
    {
        return _objects.TryDequeue(out var item) ? item : _objectGenerator();
    }

    public void Return(T item)
    {
        if (item != null && _objects.Count < _maxSize)
        {
            _objects.Enqueue(item);
        }
    }
}
