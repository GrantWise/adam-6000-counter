// Industrial.Adam.Logger - Counter Metrics Collection Implementation
// High-performance metrics collection optimized for industrial counter applications

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Industrial.Adam.Logger.Configuration;
using Industrial.Adam.Logger.Models;
using Industrial.Adam.Logger.Monitoring;
using Industrial.Adam.Logger.Performance;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Industrial.Adam.Logger.Monitoring;

/// <summary>
/// High-performance metrics collector for industrial counter applications
/// Optimized for low-latency metric recording and efficient memory usage
/// </summary>
public class CounterMetricsCollector : ICounterMetricsCollector, IDisposable
{
    private readonly ILogger<CounterMetricsCollector> _logger;
    private readonly AdamLoggerConfig _config;

    // Thread-safe metric storage
    private readonly ConcurrentDictionary<string, DeviceMetrics> _deviceMetrics = new();
    private readonly ConcurrentDictionary<string, CounterChannelMetrics> _channelMetrics = new();
    private readonly ConcurrentDictionary<string, ConcurrentQueue<TimeSpan>> _processingTimes = new();

    // Performance counters
    private long _totalReadings;
    private long _totalProcessingTimeMs;
    private long _totalRateCalculations;
    private long _totalOverflowEvents;
    private long _totalBatches;
    private long _totalBatchProcessingTimeMs;

    // Quality metrics
    private readonly ConcurrentDictionary<DataQuality, long> _qualityCounters = new();

    // Overflow tracking
    private readonly ConcurrentQueue<OverflowEvent> _recentOverflows = new();
    private readonly object _overflowLock = new();

    // System metrics
    private readonly PerformanceCounter? _cpuCounter;
    private readonly Process _currentProcess;
    private readonly DateTimeOffset _startTime;

    // Cleanup timer
    private readonly Timer _cleanupTimer;

    /// <summary>
    /// Initialize the counter metrics collector
    /// </summary>
    /// <param name="config">Logger configuration for metric settings</param>
    /// <param name="logger">Logger for diagnostic information</param>
    public CounterMetricsCollector(
        IOptions<AdamLoggerConfig> config,
        ILogger<CounterMetricsCollector> logger)
    {
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _currentProcess = Process.GetCurrentProcess();
        _startTime = DateTimeOffset.UtcNow;

        // Initialize quality counters
        foreach (DataQuality quality in Enum.GetValues<DataQuality>())
        {
            _qualityCounters[quality] = 0;
        }

        // Initialize CPU counter (Windows-specific, will be null on other platforms)
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not initialize CPU performance counter");
        }

        // Set up periodic cleanup (every 15 minutes)
        _cleanupTimer = new Timer(PerformCleanup, null,
            TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(15));

        _logger.LogInformation("Counter metrics collector initialized");
    }

    /// <summary>
    /// Record a counter reading with performance timing
    /// </summary>
    public void RecordCounterReading(AdamDataReading reading, TimeSpan processingTime)
    {
        try
        {
            Interlocked.Increment(ref _totalReadings);
            Interlocked.Add(ref _totalProcessingTimeMs, (long)processingTime.TotalMilliseconds);

            // Update device metrics
            var deviceKey = reading.DeviceId;
            _deviceMetrics.AddOrUpdate(deviceKey,
                new DeviceMetrics
                {
                    DeviceId = reading.DeviceId,
                    IsConnected = true,
                    TotalReadings = 1,
                    SuccessfulReadings = reading.Quality == DataQuality.Good ? 1 : 0,
                    FailedReadings = reading.Quality == DataQuality.Good ? 0 : 1,
                    LastSuccessfulReading = reading.Quality == DataQuality.Good ? reading.Timestamp : DateTimeOffset.MinValue,
                    LastFailedReading = reading.Quality != DataQuality.Good ? reading.Timestamp : DateTimeOffset.MinValue
                },
                (key, existing) => existing with
                {
                    TotalReadings = existing.TotalReadings + 1,
                    SuccessfulReadings = reading.Quality == DataQuality.Good ? existing.SuccessfulReadings + 1 : existing.SuccessfulReadings,
                    FailedReadings = reading.Quality != DataQuality.Good ? existing.FailedReadings + 1 : existing.FailedReadings,
                    LastSuccessfulReading = reading.Quality == DataQuality.Good ? reading.Timestamp : existing.LastSuccessfulReading,
                    LastFailedReading = reading.Quality != DataQuality.Good ? reading.Timestamp : existing.LastFailedReading
                });

            // Update channel metrics
            var channelKey = $"{reading.DeviceId}:{reading.Channel}";
            _channelMetrics.AddOrUpdate(channelKey,
                new CounterChannelMetrics
                {
                    DeviceId = reading.DeviceId,
                    ChannelNumber = reading.Channel,
                    CurrentValue = reading.RawValue,
                    CurrentRate = reading.Rate,
                    LastReading = reading.Timestamp,
                    TotalReadings = 1,
                    AverageProcessingTime = processingTime,
                    LastQuality = reading.Quality
                },
                (key, existing) => existing with
                {
                    CurrentValue = reading.RawValue,
                    CurrentRate = reading.Rate,
                    LastReading = reading.Timestamp,
                    TotalReadings = existing.TotalReadings + 1,
                    AverageProcessingTime = CalculateAverageProcessingTime(channelKey, processingTime),
                    LastQuality = reading.Quality
                });

            // Record processing time for average calculations
            RecordProcessingTime(channelKey, processingTime);

            // Update quality counters
            _qualityCounters.AddOrUpdate(reading.Quality, 1, (key, oldValue) => Interlocked.Increment(ref oldValue));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error recording counter reading metrics for device {DeviceId}, channel {Channel}",
                reading.DeviceId, reading.Channel);
        }
    }

    /// <summary>
    /// Record device connectivity status
    /// </summary>
    public void RecordDeviceConnectivity(string deviceId, bool isConnected, TimeSpan responseTime)
    {
        try
        {
            _deviceMetrics.AddOrUpdate(deviceId,
                new DeviceMetrics
                {
                    DeviceId = deviceId,
                    IsConnected = isConnected,
                    LastResponseTime = responseTime
                },
                (key, existing) => existing with
                {
                    IsConnected = isConnected,
                    LastResponseTime = responseTime
                });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error recording device connectivity for {DeviceId}", deviceId);
        }
    }

    /// <summary>
    /// Record data quality metrics
    /// </summary>
    public void RecordDataQuality(string deviceId, int channelNumber, DataQuality quality, TimeSpan validationTime)
    {
        try
        {
            _qualityCounters.AddOrUpdate(quality, 1, (key, oldValue) => Interlocked.Increment(ref oldValue));

            // Update channel quality
            var channelKey = $"{deviceId}:{channelNumber}";
            _channelMetrics.AddOrUpdate(channelKey,
                new CounterChannelMetrics
                {
                    DeviceId = deviceId,
                    ChannelNumber = channelNumber,
                    LastQuality = quality
                },
                (key, existing) => existing with { LastQuality = quality });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error recording data quality for device {DeviceId}, channel {Channel}",
                deviceId, channelNumber);
        }
    }

    /// <summary>
    /// Record performance metrics for system monitoring
    /// </summary>
    public void RecordPerformanceMetrics(PerformanceMetrics metrics)
    {
        // Performance metrics are typically aggregated elsewhere
        // This method is here for interface compliance and future extensibility
        _logger.LogDebug("Performance metrics recorded: CPU {CpuPercent}%, Memory {MemoryMB}MB",
            metrics.CpuUsagePercent, metrics.MemoryUsageMB);
    }

    /// <summary>
    /// Record memory usage metrics
    /// </summary>
    public void RecordMemoryMetrics(MemoryMetrics metrics)
    {
        // Memory metrics are typically aggregated elsewhere
        // This method is here for interface compliance and future extensibility
        _logger.LogDebug("Memory metrics recorded: {UsedMB}MB used, {AvailableMB}MB available",
            metrics.UsedMemoryMB, metrics.AvailableMemoryMB);
    }

    /// <summary>
    /// Record network performance metrics
    /// </summary>
    public void RecordNetworkMetrics(NetworkMetrics metrics)
    {
        // Network metrics are typically aggregated elsewhere
        // This method is here for interface compliance and future extensibility
        _logger.LogDebug("Network metrics recorded: {LatencyMs}ms latency, {Throughput}Mbps throughput",
            metrics.AverageLatencyMs, metrics.ThroughputMbps);
    }

    /// <summary>
    /// Record counter rate calculation performance
    /// </summary>
    public void RecordRateCalculation(
        string deviceId,
        int channelNumber,
        double? calculatedRate,
        int dataPoints,
        TimeSpan calculationTime)
    {
        try
        {
            Interlocked.Increment(ref _totalRateCalculations);

            var channelKey = $"{deviceId}:{channelNumber}";
            _channelMetrics.AddOrUpdate(channelKey,
                new CounterChannelMetrics
                {
                    DeviceId = deviceId,
                    ChannelNumber = channelNumber,
                    CurrentRate = calculatedRate
                },
                (key, existing) => existing with { CurrentRate = calculatedRate });

            _logger.LogTrace("Rate calculation recorded for {DeviceId}:{Channel}: {Rate} using {DataPoints} points in {TimeMs}ms",
                deviceId, channelNumber, calculatedRate, dataPoints, calculationTime.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error recording rate calculation for device {DeviceId}, channel {Channel}",
                deviceId, channelNumber);
        }
    }

    /// <summary>
    /// Record counter overflow detection events
    /// </summary>
    public void RecordCounterOverflow(
        string deviceId,
        int channelNumber,
        long previousValue,
        long currentValue,
        long adjustedValue)
    {
        try
        {
            Interlocked.Increment(ref _totalOverflowEvents);

            var overflowEvent = new OverflowEvent
            {
                DeviceId = deviceId,
                ChannelNumber = channelNumber,
                PreviousValue = previousValue,
                CurrentValue = currentValue,
                AdjustedValue = adjustedValue,
                Timestamp = DateTimeOffset.UtcNow
            };

            lock (_overflowLock)
            {
                _recentOverflows.Enqueue(overflowEvent);

                // Keep only last 1000 overflow events
                while (_recentOverflows.Count > 1000)
                {
                    _recentOverflows.TryDequeue(out _);
                }
            }

            // Update channel overflow count
            var channelKey = $"{deviceId}:{channelNumber}";
            _channelMetrics.AddOrUpdate(channelKey,
                new CounterChannelMetrics
                {
                    DeviceId = deviceId,
                    ChannelNumber = channelNumber,
                    OverflowEvents = 1
                },
                (key, existing) => existing with { OverflowEvents = existing.OverflowEvents + 1 });

            _logger.LogInformation("Counter overflow detected on {DeviceId}:{Channel}: {Previous} -> {Current} (adjusted: {Adjusted})",
                deviceId, channelNumber, previousValue, currentValue, adjustedValue);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error recording counter overflow for device {DeviceId}, channel {Channel}",
                deviceId, channelNumber);
        }
    }

    /// <summary>
    /// Record batch processing performance
    /// </summary>
    public void RecordBatchProcessing(int batchSize, TimeSpan processingTime, int successCount, int failureCount)
    {
        try
        {
            Interlocked.Increment(ref _totalBatches);
            Interlocked.Add(ref _totalBatchProcessingTimeMs, (long)processingTime.TotalMilliseconds);

            _logger.LogDebug("Batch processing recorded: {BatchSize} items, {SuccessCount} success, {FailureCount} failures in {TimeMs}ms",
                batchSize, successCount, failureCount, processingTime.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error recording batch processing metrics");
        }
    }

    /// <summary>
    /// Get current metrics snapshot for monitoring endpoints
    /// </summary>
    public async Task<MetricsSnapshot> GetCurrentMetricsAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                var systemHealth = GetSystemHealthMetrics();
                var performance = GetPerformanceMetrics();
                var memory = GetMemoryMetrics();
                var network = GetNetworkMetrics();

                return new MetricsSnapshot
                {
                    Timestamp = DateTimeOffset.UtcNow,
                    Performance = performance,
                    Memory = memory,
                    Network = network,
                    Devices = _deviceMetrics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                    SystemHealth = systemHealth,
                    Uptime = DateTimeOffset.UtcNow - _startTime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating metrics snapshot");
                return new MetricsSnapshot();
            }
        });
    }

    /// <summary>
    /// Get counter-specific metrics for industrial monitoring
    /// </summary>
    public async Task<CounterMetricsSnapshot> GetCounterMetricsAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                var baseMetrics = GetCurrentMetricsAsync().Result;
                var overflowMetrics = GetOverflowMetrics();
                var rateMetrics = GetRateCalculationMetrics();
                var batchMetrics = GetBatchProcessingMetrics();
                var qualityMetrics = GetDataQualityMetrics();

                return new CounterMetricsSnapshot
                {
                    Timestamp = baseMetrics.Timestamp,
                    Performance = baseMetrics.Performance,
                    Memory = baseMetrics.Memory,
                    Network = baseMetrics.Network,
                    Devices = baseMetrics.Devices,
                    SystemHealth = baseMetrics.SystemHealth,
                    Uptime = baseMetrics.Uptime,
                    CounterChannels = _channelMetrics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                    OverflowEvents = overflowMetrics,
                    RateCalculations = rateMetrics,
                    BatchProcessing = batchMetrics,
                    DataQuality = qualityMetrics
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating counter metrics snapshot");
                return new CounterMetricsSnapshot();
            }
        });
    }

    /// <summary>
    /// Reset all metrics counters
    /// </summary>
    public void ResetMetrics()
    {
        try
        {
            _deviceMetrics.Clear();
            _channelMetrics.Clear();
            _processingTimes.Clear();

            Interlocked.Exchange(ref _totalReadings, 0);
            Interlocked.Exchange(ref _totalProcessingTimeMs, 0);
            Interlocked.Exchange(ref _totalRateCalculations, 0);
            Interlocked.Exchange(ref _totalOverflowEvents, 0);
            Interlocked.Exchange(ref _totalBatches, 0);
            Interlocked.Exchange(ref _totalBatchProcessingTimeMs, 0);

            foreach (var quality in Enum.GetValues<DataQuality>())
            {
                _qualityCounters.AddOrUpdate(quality, 0, (key, oldValue) => 0);
            }

            lock (_overflowLock)
            {
                while (_recentOverflows.TryDequeue(out _))
                { }
            }

            _logger.LogInformation("All metrics counters reset");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting metrics");
        }
    }

    /// <summary>
    /// Record processing time for average calculations
    /// </summary>
    private void RecordProcessingTime(string channelKey, TimeSpan processingTime)
    {
        var times = _processingTimes.GetOrAdd(channelKey, _ => new ConcurrentQueue<TimeSpan>());
        times.Enqueue(processingTime);

        // Keep only last 100 processing times per channel
        while (times.Count > 100)
        {
            times.TryDequeue(out _);
        }
    }

    /// <summary>
    /// Calculate average processing time for a channel
    /// </summary>
    private TimeSpan CalculateAverageProcessingTime(string channelKey, TimeSpan newTime)
    {
        if (_processingTimes.TryGetValue(channelKey, out var times))
        {
            var timesList = times.ToArray();
            if (timesList.Length > 0)
            {
                var avgMs = timesList.Average(t => t.TotalMilliseconds);
                return TimeSpan.FromMilliseconds(avgMs);
            }
        }
        return newTime;
    }

    /// <summary>
    /// Get system health metrics
    /// </summary>
    private SystemHealthMetrics GetSystemHealthMetrics()
    {
        try
        {
            var cpuUsage = GetCpuUsage();
            var memoryUsage = GetMemoryUsagePercent();
            var activeThreads = _currentProcess.Threads.Count;
            var uptime = DateTimeOffset.UtcNow - _startTime;

            var healthStatus = "Healthy";
            var alerts = new List<string>();

            if (cpuUsage > Constants.DefaultCpuUsageThresholdPercent)
            {
                healthStatus = "Degraded";
                alerts.Add($"High CPU usage: {cpuUsage:F1}%");
            }

            if (memoryUsage > Constants.DefaultMemoryUsageThresholdPercent)
            {
                healthStatus = "Degraded";
                alerts.Add($"High memory usage: {memoryUsage:F1}%");
            }

            return new SystemHealthMetrics
            {
                CpuUsagePercent = cpuUsage,
                MemoryUsagePercent = memoryUsage,
                ActiveThreads = activeThreads,
                SystemUptime = uptime,
                HealthStatus = healthStatus,
                ActiveAlerts = alerts
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting system health metrics");
            return new SystemHealthMetrics();
        }
    }

    /// <summary>
    /// Get CPU usage percentage
    /// </summary>
    private double GetCpuUsage()
    {
        try
        {
            if (_cpuCounter != null && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return _cpuCounter.NextValue();
            }

            // Fallback for non-Windows platforms
            return (_currentProcess.TotalProcessorTime.TotalMilliseconds / Environment.ProcessorCount) /
                   (DateTimeOffset.UtcNow - _startTime).TotalMilliseconds * 100;
        }
        catch
        {
            return 0.0;
        }
    }

    /// <summary>
    /// Get memory usage percentage
    /// </summary>
    private double GetMemoryUsagePercent()
    {
        try
        {
            var usedMemory = GC.GetTotalMemory(false);
            var workingSet = _currentProcess.WorkingSet64;
            return workingSet > 0 ? (double)usedMemory / workingSet * 100 : 0;
        }
        catch
        {
            return 0.0;
        }
    }

    /// <summary>
    /// Get performance metrics
    /// </summary>
    private PerformanceMetrics GetPerformanceMetrics()
    {
        var totalReadings = Interlocked.Read(ref _totalReadings);
        var totalProcessingTimeMs = Interlocked.Read(ref _totalProcessingTimeMs);

        var avgResponseTime = totalReadings > 0 ? (double)totalProcessingTimeMs / totalReadings : 0;
        var dataProcessingRate = totalProcessingTimeMs > 0 ? totalReadings / (totalProcessingTimeMs / 1000.0) : 0;

        return new PerformanceMetrics
        {
            CpuUsagePercent = GetCpuUsage(),
            MemoryUsageMB = GC.GetTotalMemory(false) / (1024 * 1024),
            DataProcessingRatePerSecond = dataProcessingRate,
            AverageResponseTimeMs = avgResponseTime,
            ActiveConnections = _deviceMetrics.Count(d => d.Value.IsConnected),
            QueuedOperations = 0, // Would be implemented based on actual queue monitoring
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Get memory metrics
    /// </summary>
    private MemoryMetrics GetMemoryMetrics()
    {
        var totalMemory = GC.GetTotalMemory(false);
        var workingSet = _currentProcess.WorkingSet64;

        return new MemoryMetrics
        {
            TotalMemoryMB = workingSet / (1024 * 1024),
            UsedMemoryMB = totalMemory / (1024 * 1024),
            AvailableMemoryMB = (workingSet - totalMemory) / (1024 * 1024),
            MemoryUsagePercent = GetMemoryUsagePercent(),
            GarbageCollectionCount = GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2),
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Get network metrics
    /// </summary>
    private NetworkMetrics GetNetworkMetrics()
    {
        return new NetworkMetrics
        {
            ActiveConnections = _deviceMetrics.Count(d => d.Value.IsConnected),
            FailedConnections = _deviceMetrics.Count(d => !d.Value.IsConnected),
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Get overflow metrics
    /// </summary>
    private CounterOverflowMetrics GetOverflowMetrics()
    {
        lock (_overflowLock)
        {
            var overflows = _recentOverflows.ToArray();
            var overflowsByDevice = overflows.GroupBy(o => o.DeviceId).ToDictionary(g => g.Key, g => g.Count());
            var overflowsByChannel = overflows.GroupBy(o => $"{o.DeviceId}:{o.ChannelNumber}").ToDictionary(g => g.Key, g => g.Count());

            return new CounterOverflowMetrics
            {
                TotalOverflowEvents = (int)Interlocked.Read(ref _totalOverflowEvents),
                LastOverflowEvent = overflows.Length > 0 ? overflows.Max(o => o.Timestamp) : DateTimeOffset.MinValue,
                OverflowsByDevice = overflowsByDevice,
                OverflowsByChannel = overflowsByChannel
            };
        }
    }

    /// <summary>
    /// Get rate calculation metrics
    /// </summary>
    private RateCalculationMetrics GetRateCalculationMetrics()
    {
        return new RateCalculationMetrics
        {
            TotalCalculations = (int)Interlocked.Read(ref _totalRateCalculations),
            SuccessfulCalculations = (int)Interlocked.Read(ref _totalRateCalculations), // Simplified
            FailedCalculations = 0 // Would need to track separately
        };
    }

    /// <summary>
    /// Get batch processing metrics
    /// </summary>
    private BatchProcessingMetrics GetBatchProcessingMetrics()
    {
        var totalBatches = Interlocked.Read(ref _totalBatches);
        var totalBatchTimeMs = Interlocked.Read(ref _totalBatchProcessingTimeMs);

        return new BatchProcessingMetrics
        {
            TotalBatches = (int)totalBatches,
            AverageBatchProcessingTime = totalBatches > 0 ? TimeSpan.FromMilliseconds(totalBatchTimeMs / totalBatches) : TimeSpan.Zero,
            MaxBatchProcessingTime = TimeSpan.Zero // Would need to track separately
        };
    }

    /// <summary>
    /// Get data quality metrics
    /// </summary>
    private DataQualityMetrics GetDataQualityMetrics()
    {
        var qualityDistribution = _qualityCounters.ToDictionary(kvp => kvp.Key, kvp => (int)kvp.Value);
        var totalReadings = qualityDistribution.Values.Sum();

        return new DataQualityMetrics
        {
            TotalReadings = totalReadings,
            GoodQualityReadings = qualityDistribution.GetValueOrDefault(DataQuality.Good, 0),
            UncertainQualityReadings = qualityDistribution.GetValueOrDefault(DataQuality.Uncertain, 0),
            BadQualityReadings = qualityDistribution.GetValueOrDefault(DataQuality.Bad, 0),
            ConfigurationErrorReadings = qualityDistribution.GetValueOrDefault(DataQuality.ConfigurationError, 0),
            QualityDistribution = qualityDistribution
        };
    }

    /// <summary>
    /// Perform periodic cleanup of old metrics
    /// </summary>
    private void PerformCleanup(object? state)
    {
        try
        {
            _logger.LogDebug("Performing metrics cleanup");

            // Clean up old processing times
            foreach (var kvp in _processingTimes.ToArray())
            {
                var times = kvp.Value;
                while (times.Count > 50) // Reduce to 50 instead of 100
                {
                    times.TryDequeue(out _);
                }
            }

            // Clean up old overflow events
            lock (_overflowLock)
            {
                while (_recentOverflows.Count > 500) // Reduce to 500 instead of 1000
                {
                    _recentOverflows.TryDequeue(out _);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during metrics cleanup");
        }
    }

    /// <summary>
    /// Dispose of resources
    /// </summary>
    public void Dispose()
    {
        try
        {
            _cleanupTimer?.Dispose();
            _cpuCounter?.Dispose();
            _currentProcess?.Dispose();

            _logger.LogInformation("Counter metrics collector disposed. Final stats: {TotalReadings} readings, {TotalOverflows} overflows",
                Interlocked.Read(ref _totalReadings), Interlocked.Read(ref _totalOverflowEvents));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during metrics collector disposal");
        }
    }
}

/// <summary>
/// Counter overflow event for tracking
/// </summary>
internal record OverflowEvent
{
    public string DeviceId { get; init; } = string.Empty;
    public int ChannelNumber { get; init; }
    public long PreviousValue { get; init; }
    public long CurrentValue { get; init; }
    public long AdjustedValue { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}
