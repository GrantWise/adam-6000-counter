using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Infrastructure.Monitoring;

/// <summary>
/// Performance metrics collection for OEE operations
/// Provides comprehensive monitoring of calculation performance and system health
/// </summary>
public interface IOeePerformanceMetrics
{
    /// <summary>
    /// Track OEE calculation performance
    /// </summary>
    /// <param name="calculationType">Type of calculation (Availability, Performance, Quality, Overall)</param>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="duration">Calculation duration</param>
    /// <param name="dataPoints">Number of data points processed</param>
    /// <param name="success">Whether calculation succeeded</param>
    public void TrackCalculation(string calculationType, string deviceId, TimeSpan duration, int dataPoints, bool success);

    /// <summary>
    /// Track query performance
    /// </summary>
    /// <param name="queryType">Type of query</param>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="duration">Query duration</param>
    /// <param name="resultCount">Number of results returned</param>
    public void TrackQuery(string queryType, string deviceId, TimeSpan duration, int resultCount);

    /// <summary>
    /// Track data volume metrics
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="dataPoints">Number of data points</param>
    /// <param name="timeSpan">Time span covered</param>
    public void TrackDataVolume(string deviceId, int dataPoints, TimeSpan timeSpan);

    /// <summary>
    /// Get performance statistics for monitoring
    /// </summary>
    /// <param name="since">Get stats since this time</param>
    /// <returns>Performance statistics</returns>
    public Task<PerformanceStatistics> GetStatisticsAsync(DateTimeOffset? since = null);

    /// <summary>
    /// Create a scoped performance tracker
    /// </summary>
    /// <param name="operationType">Type of operation</param>
    /// <param name="deviceId">Device identifier</param>
    /// <returns>Disposable performance tracker</returns>
    public IDisposable TrackOperation(string operationType, string deviceId);
}

/// <summary>
/// Implementation of OEE performance metrics
/// </summary>
public sealed class OeePerformanceMetrics : IOeePerformanceMetrics, IDisposable
{
    private readonly Meter _meter;
    private readonly ILogger<OeePerformanceMetrics> _logger;

    // Calculation metrics
    private readonly Counter<long> _calculationCounter;
    private readonly Histogram<double> _calculationDuration;
    private readonly Counter<long> _calculationErrors;
    private readonly Histogram<int> _dataPointsProcessed;

    // Query metrics
    private readonly Counter<long> _queryCounter;
    private readonly Histogram<double> _queryDuration;
    private readonly Histogram<int> _queryResults;

    // System metrics
    private readonly Gauge<double> _memoryUsage;
    private readonly Gauge<int> _activeTasks;

    // Performance tracking
    private readonly ConcurrentDictionary<string, PerformanceData> _performanceData = new();
    private const int MaxPerformanceEntries = 1000;

    /// <summary>
    /// Initialize OEE performance metrics
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public OeePerformanceMetrics(ILogger<OeePerformanceMetrics> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _meter = new Meter("Industrial.Adam.Oee.Performance", "1.0.0");

        // Calculation metrics
        _calculationCounter = _meter.CreateCounter<long>(
            "oee_calculations_total",
            "count",
            "Total number of OEE calculations performed");

        _calculationDuration = _meter.CreateHistogram<double>(
            "oee_calculation_duration_ms",
            "milliseconds",
            "Duration of OEE calculations in milliseconds");

        _calculationErrors = _meter.CreateCounter<long>(
            "oee_calculation_errors_total",
            "count",
            "Total number of OEE calculation errors");

        _dataPointsProcessed = _meter.CreateHistogram<int>(
            "oee_data_points_processed",
            "count",
            "Number of data points processed in calculations");

        // Query metrics
        _queryCounter = _meter.CreateCounter<long>(
            "oee_queries_total",
            "count",
            "Total number of database queries executed");

        _queryDuration = _meter.CreateHistogram<double>(
            "oee_query_duration_ms",
            "milliseconds",
            "Duration of database queries in milliseconds");

        _queryResults = _meter.CreateHistogram<int>(
            "oee_query_results",
            "count",
            "Number of results returned by queries");

        // System metrics
        _memoryUsage = _meter.CreateGauge<double>(
            "oee_memory_usage_mb",
            "megabytes",
            "Current memory usage in megabytes");

        _activeTasks = _meter.CreateGauge<int>(
            "oee_active_tasks",
            "count",
            "Number of active OEE processing tasks");

        // Start background metrics collection
        _ = Task.Run(CollectSystemMetricsAsync);

        _logger.LogInformation("OEE Performance Metrics initialized");
    }

    /// <inheritdoc />
    public void TrackCalculation(string calculationType, string deviceId, TimeSpan duration, int dataPoints, bool success)
    {
        var tags = new TagList
        {
            { "calculation_type", calculationType },
            { "device_id", deviceId },
            { "success", success.ToString().ToLowerInvariant() }
        };

        _calculationCounter.Add(1, tags);
        _calculationDuration.Record(duration.TotalMilliseconds, tags);
        _dataPointsProcessed.Record(dataPoints, tags);

        if (!success)
        {
            _calculationErrors.Add(1, tags);
        }

        // Track performance data for statistics
        var key = $"{calculationType}_{deviceId}";
        _performanceData.AddOrUpdate(key,
            new PerformanceData { TotalCalculations = 1, TotalDuration = duration, LastCalculation = DateTimeOffset.UtcNow },
            (_, existing) => new PerformanceData
            {
                TotalCalculations = existing.TotalCalculations + 1,
                TotalDuration = existing.TotalDuration + duration,
                LastCalculation = DateTimeOffset.UtcNow
            });

        // Log performance warnings
        if (duration.TotalSeconds > 5)
        {
            _logger.LogWarning(
                "Slow OEE calculation detected: {CalculationType} for {DeviceId} took {Duration}ms with {DataPoints} data points",
                calculationType, deviceId, duration.TotalMilliseconds, dataPoints);
        }

        _logger.LogDebug(
            "OEE calculation tracked: {CalculationType} for {DeviceId} - {Duration}ms, {DataPoints} points, Success: {Success}",
            calculationType, deviceId, duration.TotalMilliseconds, dataPoints, success);
    }

    /// <inheritdoc />
    public void TrackQuery(string queryType, string deviceId, TimeSpan duration, int resultCount)
    {
        var tags = new TagList
        {
            { "query_type", queryType },
            { "device_id", deviceId },
            { "result_size_bucket", GetResultSizeBucket(resultCount) }
        };

        _queryCounter.Add(1, tags);
        _queryDuration.Record(duration.TotalMilliseconds, tags);
        _queryResults.Record(resultCount, tags);

        // Log slow queries
        if (duration.TotalMilliseconds > 1000)
        {
            _logger.LogWarning(
                "Slow query detected: {QueryType} for {DeviceId} took {Duration}ms and returned {ResultCount} results",
                queryType, deviceId, duration.TotalMilliseconds, resultCount);
        }

        _logger.LogDebug(
            "Query tracked: {QueryType} for {DeviceId} - {Duration}ms, {ResultCount} results",
            queryType, deviceId, duration.TotalMilliseconds, resultCount);
    }

    /// <inheritdoc />
    public void TrackDataVolume(string deviceId, int dataPoints, TimeSpan timeSpan)
    {
        var dataRate = dataPoints / Math.Max(timeSpan.TotalMinutes, 1);

        var tags = new TagList
        {
            { "device_id", deviceId },
            { "volume_bucket", GetVolumeBucket(dataPoints) }
        };

        // Log unusual data volumes
        if (dataPoints > 10000)
        {
            _logger.LogWarning(
                "High data volume detected for {DeviceId}: {DataPoints} points over {TimeSpan} ({Rate:F1} points/minute)",
                deviceId, dataPoints, timeSpan, dataRate);
        }
        else if (dataPoints < 10 && timeSpan.TotalMinutes > 60)
        {
            _logger.LogWarning(
                "Low data volume detected for {DeviceId}: {DataPoints} points over {TimeSpan} ({Rate:F1} points/minute)",
                deviceId, dataPoints, timeSpan, dataRate);
        }

        _logger.LogDebug(
            "Data volume tracked for {DeviceId}: {DataPoints} points over {TimeSpan} ({Rate:F1} points/minute)",
            deviceId, dataPoints, timeSpan, dataRate);
    }

    /// <inheritdoc />
    public async Task<PerformanceStatistics> GetStatisticsAsync(DateTimeOffset? since = null)
    {
        var cutoff = since ?? DateTimeOffset.UtcNow.AddHours(-24);

        var relevantData = _performanceData.Values
            .Where(p => p.LastCalculation >= cutoff)
            .ToList();

        var stats = new PerformanceStatistics
        {
            TotalCalculations = relevantData.Sum(p => p.TotalCalculations),
            AverageCalculationTime = relevantData.Any()
                ? TimeSpan.FromTicks((long)relevantData.Average(p => p.TotalDuration.Ticks))
                : TimeSpan.Zero,
            MaxCalculationTime = relevantData.Any()
                ? relevantData.Max(p => p.TotalDuration)
                : TimeSpan.Zero,
            ActiveDevices = relevantData.Count,
            CalculationsPerMinute = relevantData.Any()
                ? relevantData.Sum(p => p.TotalCalculations) / Math.Max((DateTimeOffset.UtcNow - cutoff).TotalMinutes, 1)
                : 0,
            MemoryUsageMB = GC.GetTotalMemory(false) / 1024.0 / 1024.0,
            Since = cutoff
        };

        return await Task.FromResult(stats);
    }

    /// <inheritdoc />
    public IDisposable TrackOperation(string operationType, string deviceId)
    {
        return new ScopedOperationTracker(this, operationType, deviceId);
    }

    /// <summary>
    /// Collect system metrics periodically
    /// </summary>
    private async Task CollectSystemMetricsAsync()
    {
        while (true)
        {
            try
            {
                var memoryMB = GC.GetTotalMemory(false) / 1024.0 / 1024.0;
                _memoryUsage.Record(memoryMB);

                var activeTasks = Process.GetCurrentProcess().Threads.Count;
                _activeTasks.Record(activeTasks);

                // Clean up old performance data
                CleanupOldPerformanceData();

                await Task.Delay(TimeSpan.FromMinutes(1));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error collecting system metrics");
                await Task.Delay(TimeSpan.FromMinutes(5));
            }
        }
    }

    /// <summary>
    /// Clean up old performance data to prevent memory growth
    /// </summary>
    private void CleanupOldPerformanceData()
    {
        if (_performanceData.Count <= MaxPerformanceEntries)
            return;

        var cutoff = DateTimeOffset.UtcNow.AddHours(-24);
        var keysToRemove = _performanceData
            .Where(kvp => kvp.Value.LastCalculation < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _performanceData.TryRemove(key, out _);
        }

        _logger.LogDebug("Cleaned up {Count} old performance data entries", keysToRemove.Count);
    }

    /// <summary>
    /// Get result size bucket for grouping
    /// </summary>
    /// <param name="resultCount">Number of results</param>
    /// <returns>Bucket label</returns>
    private static string GetResultSizeBucket(int resultCount)
    {
        return resultCount switch
        {
            <= 10 => "small",
            <= 100 => "medium",
            <= 1000 => "large",
            _ => "very_large"
        };
    }

    /// <summary>
    /// Get volume bucket for grouping
    /// </summary>
    /// <param name="dataPoints">Number of data points</param>
    /// <returns>Bucket label</returns>
    private static string GetVolumeBucket(int dataPoints)
    {
        return dataPoints switch
        {
            <= 100 => "low",
            <= 1000 => "normal",
            <= 10000 => "high",
            _ => "very_high"
        };
    }

    /// <summary>
    /// Dispose metrics resources
    /// </summary>
    public void Dispose()
    {
        _meter?.Dispose();
        _logger.LogInformation("OEE Performance Metrics disposed");
    }
}

/// <summary>
/// Scoped operation tracker for automatic performance measurement
/// </summary>
internal sealed class ScopedOperationTracker : IDisposable
{
    private readonly OeePerformanceMetrics _metrics;
    private readonly string _operationType;
    private readonly string _deviceId;
    private readonly Stopwatch _stopwatch;
    private bool _disposed;

    public ScopedOperationTracker(OeePerformanceMetrics metrics, string operationType, string deviceId)
    {
        _metrics = metrics;
        _operationType = operationType;
        _deviceId = deviceId;
        _stopwatch = Stopwatch.StartNew();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _stopwatch.Stop();
        _metrics.TrackCalculation(_operationType, _deviceId, _stopwatch.Elapsed, 0, true);
        _disposed = true;
    }
}

/// <summary>
/// Performance data for tracking calculations
/// </summary>
internal sealed record PerformanceData
{
    public int TotalCalculations { get; init; }
    public TimeSpan TotalDuration { get; init; }
    public DateTimeOffset LastCalculation { get; init; }
}

/// <summary>
/// Performance statistics for monitoring
/// </summary>
public sealed record PerformanceStatistics
{
    public int TotalCalculations { get; init; }
    public TimeSpan AverageCalculationTime { get; init; }
    public TimeSpan MaxCalculationTime { get; init; }
    public int ActiveDevices { get; init; }
    public double CalculationsPerMinute { get; init; }
    public double MemoryUsageMB { get; init; }
    public DateTimeOffset Since { get; init; }
}
