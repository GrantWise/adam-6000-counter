// Industrial.Adam.Logger - Metrics Collection Interfaces
// Interfaces for collecting and exposing performance metrics for monitoring systems

using Industrial.Adam.Logger.Models;
using Industrial.Adam.Logger.Performance;

namespace Industrial.Adam.Logger.Monitoring;

/// <summary>
/// Interface for collecting application metrics for monitoring and alerting systems
/// Provides methods to track performance, health, and operational metrics
/// </summary>
public interface IMetricsCollector
{
    /// <summary>
    /// Record a counter reading with performance timing
    /// </summary>
    /// <param name="reading">Counter data reading to record</param>
    /// <param name="processingTime">Time taken to process the reading</param>
    void RecordCounterReading(AdamDataReading reading, TimeSpan processingTime);

    /// <summary>
    /// Record device connectivity status
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="isConnected">Whether the device is currently connected</param>
    /// <param name="responseTime">Device response time for health check</param>
    void RecordDeviceConnectivity(string deviceId, bool isConnected, TimeSpan responseTime);

    /// <summary>
    /// Record data quality metrics
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="channelNumber">Channel number</param>
    /// <param name="quality">Data quality assessment</param>
    /// <param name="validationTime">Time taken for data validation</param>
    void RecordDataQuality(string deviceId, int channelNumber, DataQuality quality, TimeSpan validationTime);

    /// <summary>
    /// Record performance metrics for system monitoring
    /// </summary>
    /// <param name="metrics">Performance metrics to record</param>
    void RecordPerformanceMetrics(PerformanceMetrics metrics);

    /// <summary>
    /// Record memory usage metrics
    /// </summary>
    /// <param name="metrics">Memory usage metrics to record</param>
    void RecordMemoryMetrics(MemoryMetrics metrics);

    /// <summary>
    /// Record network performance metrics
    /// </summary>
    /// <param name="metrics">Network performance metrics to record</param>
    void RecordNetworkMetrics(NetworkMetrics metrics);

    /// <summary>
    /// Get current metrics snapshot for monitoring endpoints
    /// </summary>
    /// <returns>Current metrics formatted for monitoring consumption</returns>
    Task<MetricsSnapshot> GetCurrentMetricsAsync();

    /// <summary>
    /// Reset all metrics counters (useful for testing or maintenance)
    /// </summary>
    void ResetMetrics();
}

/// <summary>
/// Interface for counter-specific metrics collection and analysis
/// Focuses on industrial counter application monitoring patterns
/// </summary>
public interface ICounterMetricsCollector : IMetricsCollector
{
    /// <summary>
    /// Record counter rate calculation performance
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="channelNumber">Channel number</param>
    /// <param name="calculatedRate">Calculated rate value</param>
    /// <param name="dataPoints">Number of data points used in calculation</param>
    /// <param name="calculationTime">Time taken for rate calculation</param>
    void RecordRateCalculation(
        string deviceId,
        int channelNumber,
        double? calculatedRate,
        int dataPoints,
        TimeSpan calculationTime);

    /// <summary>
    /// Record counter overflow detection events
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="channelNumber">Channel number</param>
    /// <param name="previousValue">Previous counter value</param>
    /// <param name="currentValue">Current counter value that triggered overflow</param>
    /// <param name="adjustedValue">Overflow-adjusted value</param>
    void RecordCounterOverflow(
        string deviceId,
        int channelNumber,
        long previousValue,
        long currentValue,
        long adjustedValue);

    /// <summary>
    /// Record batch processing performance
    /// </summary>
    /// <param name="batchSize">Number of readings in the batch</param>
    /// <param name="processingTime">Total time to process the batch</param>
    /// <param name="successCount">Number of successfully processed readings</param>
    /// <param name="failureCount">Number of failed processing attempts</param>
    void RecordBatchProcessing(int batchSize, TimeSpan processingTime, int successCount, int failureCount);

    /// <summary>
    /// Get counter-specific metrics for industrial monitoring
    /// </summary>
    /// <returns>Counter metrics optimized for industrial dashboards</returns>
    Task<CounterMetricsSnapshot> GetCounterMetricsAsync();
}

/// <summary>
/// Interface for real-time metrics streaming to monitoring systems
/// Provides live updates for dashboard and alerting systems
/// </summary>
public interface ILiveMetricsStreamer
{
    /// <summary>
    /// Start streaming metrics to connected clients
    /// </summary>
    /// <param name="updateInterval">Interval between metric updates</param>
    /// <returns>Task that completes when streaming is started</returns>
    Task StartStreamingAsync(TimeSpan updateInterval);

    /// <summary>
    /// Stop streaming metrics
    /// </summary>
    /// <returns>Task that completes when streaming is stopped</returns>
    Task StopStreamingAsync();

    /// <summary>
    /// Subscribe to real-time metrics updates
    /// </summary>
    /// <param name="callback">Callback function to receive metric updates</param>
    /// <returns>Subscription ID for managing the subscription</returns>
    string Subscribe(Func<MetricsSnapshot, Task> callback);

    /// <summary>
    /// Unsubscribe from metrics updates
    /// </summary>
    /// <param name="subscriptionId">Subscription ID to remove</param>
    /// <returns>True if subscription was found and removed</returns>
    bool Unsubscribe(string subscriptionId);

    /// <summary>
    /// Get number of active subscribers
    /// </summary>
    /// <returns>Number of active metric subscriptions</returns>
    int GetActiveSubscriberCount();
}

/// <summary>
/// Comprehensive metrics snapshot for monitoring systems
/// </summary>
public record MetricsSnapshot
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public PerformanceMetrics Performance { get; init; } = new();
    public MemoryMetrics Memory { get; init; } = new();
    public NetworkMetrics Network { get; init; } = new();
    public Dictionary<string, DeviceMetrics> Devices { get; init; } = new();
    public SystemHealthMetrics SystemHealth { get; init; } = new();
    public string Version { get; init; } = "1.0.0";
    public TimeSpan Uptime { get; init; }
}

/// <summary>
/// Counter-specific metrics snapshot for industrial monitoring
/// </summary>
public record CounterMetricsSnapshot : MetricsSnapshot
{
    public Dictionary<string, CounterChannelMetrics> CounterChannels { get; init; } = new();
    public CounterOverflowMetrics OverflowEvents { get; init; } = new();
    public RateCalculationMetrics RateCalculations { get; init; } = new();
    public BatchProcessingMetrics BatchProcessing { get; init; } = new();
    public DataQualityMetrics DataQuality { get; init; } = new();
}

/// <summary>
/// Metrics for individual devices
/// </summary>
public record DeviceMetrics
{
    public string DeviceId { get; init; } = string.Empty;
    public bool IsConnected { get; init; }
    public TimeSpan LastResponseTime { get; init; }
    public int TotalReadings { get; init; }
    public int SuccessfulReadings { get; init; }
    public int FailedReadings { get; init; }
    public double SuccessRate => TotalReadings > 0 ? (double)SuccessfulReadings / TotalReadings * 100 : 0;
    public DateTimeOffset LastSuccessfulReading { get; init; }
    public DateTimeOffset LastFailedReading { get; init; }
    public List<string> RecentErrors { get; init; } = new();
}

/// <summary>
/// Metrics for individual counter channels
/// </summary>
public record CounterChannelMetrics
{
    public string DeviceId { get; init; } = string.Empty;
    public int ChannelNumber { get; init; }
    public long CurrentValue { get; init; }
    public double? CurrentRate { get; init; }
    public DateTimeOffset LastReading { get; init; }
    public int TotalReadings { get; init; }
    public int OverflowEvents { get; init; }
    public TimeSpan AverageProcessingTime { get; init; }
    public DataQuality LastQuality { get; init; }
}

/// <summary>
/// Counter overflow tracking metrics
/// </summary>
public record CounterOverflowMetrics
{
    public int TotalOverflowEvents { get; init; }
    public DateTimeOffset LastOverflowEvent { get; init; }
    public Dictionary<string, int> OverflowsByDevice { get; init; } = new();
    public Dictionary<string, int> OverflowsByChannel { get; init; } = new();
}

/// <summary>
/// Rate calculation performance metrics
/// </summary>
public record RateCalculationMetrics
{
    public int TotalCalculations { get; init; }
    public TimeSpan AverageCalculationTime { get; init; }
    public TimeSpan MaxCalculationTime { get; init; }
    public int SuccessfulCalculations { get; init; }
    public int FailedCalculations { get; init; }
    public double AverageDataPointsUsed { get; init; }
}

/// <summary>
/// Batch processing performance metrics
/// </summary>
public record BatchProcessingMetrics
{
    public int TotalBatches { get; init; }
    public int AverageBatchSize { get; init; }
    public TimeSpan AverageBatchProcessingTime { get; init; }
    public TimeSpan MaxBatchProcessingTime { get; init; }
    public double BatchSuccessRate { get; init; }
    public int TotalTimeouts { get; init; }
}

/// <summary>
/// Data quality tracking metrics
/// </summary>
public record DataQualityMetrics
{
    public int TotalReadings { get; init; }
    public int GoodQualityReadings { get; init; }
    public int UncertainQualityReadings { get; init; }
    public int BadQualityReadings { get; init; }
    public int ConfigurationErrorReadings { get; init; }
    public double GoodQualityPercentage => TotalReadings > 0 ? (double)GoodQualityReadings / TotalReadings * 100 : 0;
    public Dictionary<DataQuality, int> QualityDistribution { get; init; } = new();
}

/// <summary>
/// System health metrics for overall monitoring
/// </summary>
public record SystemHealthMetrics
{
    public double CpuUsagePercent { get; init; }
    public double MemoryUsagePercent { get; init; }
    public double DiskUsagePercent { get; init; }
    public int ActiveThreads { get; init; }
    public int TotalConnections { get; init; }
    public TimeSpan SystemUptime { get; init; }
    public string HealthStatus { get; init; } = "Unknown";
    public List<string> ActiveAlerts { get; init; } = new();
}
