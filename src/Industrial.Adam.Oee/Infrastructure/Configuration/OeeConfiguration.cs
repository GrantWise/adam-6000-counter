namespace Industrial.Adam.Oee.Infrastructure.Configuration;

/// <summary>
/// Configuration for OEE application settings
/// </summary>
public class OeeConfiguration
{
    /// <summary>
    /// Database connection settings
    /// </summary>
    public OeeDatabaseSettings Database { get; set; } = new();

    /// <summary>
    /// Cache settings
    /// </summary>
    public OeeCacheSettings Cache { get; set; } = new();

    /// <summary>
    /// Resilience policy settings
    /// </summary>
    public OeeResilienceSettings Resilience { get; set; } = new();

    /// <summary>
    /// Performance monitoring settings
    /// </summary>
    public OeePerformanceSettings Performance { get; set; } = new();

    /// <summary>
    /// Stoppage detection and monitoring settings
    /// </summary>
    public OeeStoppageSettings Stoppage { get; set; } = new();

    /// <summary>
    /// SignalR real-time notification settings
    /// </summary>
    public OeeSignalRSettings SignalR { get; set; } = new();
}

/// <summary>
/// Database configuration settings
/// </summary>
public class OeeDatabaseSettings
{
    /// <summary>
    /// Database connection string
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Command timeout in seconds
    /// </summary>
    public int CommandTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Whether to enable connection pooling
    /// </summary>
    public bool EnableConnectionPooling { get; set; } = true;

    /// <summary>
    /// Maximum number of connections in pool
    /// </summary>
    public int MaxPoolSize { get; set; } = 100;
}

/// <summary>
/// Cache configuration settings
/// </summary>
public class OeeCacheSettings
{
    /// <summary>
    /// Default cache expiration in minutes
    /// </summary>
    public int DefaultExpirationMinutes { get; set; } = 5;

    /// <summary>
    /// OEE metrics cache expiration in minutes
    /// </summary>
    public int OeeMetricsExpirationMinutes { get; set; } = 2;

    /// <summary>
    /// Work order cache expiration in minutes
    /// </summary>
    public int WorkOrderExpirationMinutes { get; set; } = 10;

    /// <summary>
    /// Device status cache expiration in minutes
    /// </summary>
    public int DeviceStatusExpirationMinutes { get; set; } = 1;
}

/// <summary>
/// Resilience policy configuration settings
/// </summary>
public class OeeResilienceSettings
{
    /// <summary>
    /// Database retry policy settings
    /// </summary>
    public RetryPolicySettings DatabaseRetry { get; set; } = new();

    /// <summary>
    /// Circuit breaker policy settings
    /// </summary>
    public CircuitBreakerPolicySettings CircuitBreaker { get; set; } = new();
}

/// <summary>
/// Retry policy settings
/// </summary>
public class RetryPolicySettings
{
    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Base delay between retries in milliseconds
    /// </summary>
    public int BaseDelayMs { get; set; } = 1000;

    /// <summary>
    /// Whether to use exponential backoff
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = true;

    /// <summary>
    /// Maximum delay between retries in milliseconds
    /// </summary>
    public int MaxDelayMs { get; set; } = 30000;
}

/// <summary>
/// Circuit breaker policy settings
/// </summary>
public class CircuitBreakerPolicySettings
{
    /// <summary>
    /// Number of exceptions before opening circuit
    /// </summary>
    public int ExceptionsAllowedBeforeBreaking { get; set; } = 5;

    /// <summary>
    /// Duration circuit stays open in seconds
    /// </summary>
    public int DurationOfBreakSeconds { get; set; } = 30;

    /// <summary>
    /// Sampling duration in seconds
    /// </summary>
    public int SamplingDurationSeconds { get; set; } = 60;

    /// <summary>
    /// Minimum throughput before circuit breaking is considered
    /// </summary>
    public int MinimumThroughput { get; set; } = 10;
}

/// <summary>
/// Performance monitoring settings
/// </summary>
public class OeePerformanceSettings
{
    /// <summary>
    /// Whether performance monitoring is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether to collect detailed query metrics
    /// </summary>
    public bool EnableDetailedMetrics { get; set; } = false;

    /// <summary>
    /// Slow query threshold in milliseconds
    /// </summary>
    public int SlowQueryThresholdMs { get; set; } = 1000;

    /// <summary>
    /// Whether to log slow queries
    /// </summary>
    public bool LogSlowQueries { get; set; } = true;
}

/// <summary>
/// Stoppage detection and monitoring configuration settings
/// </summary>
public class OeeStoppageSettings
{
    /// <summary>
    /// Whether automatic stoppage detection is enabled
    /// </summary>
    public bool DetectionEnabled { get; set; } = true;

    /// <summary>
    /// Default detection threshold in minutes
    /// </summary>
    public int DefaultDetectionThresholdMinutes { get; set; } = 5;

    /// <summary>
    /// Default classification threshold in minutes
    /// </summary>
    public int DefaultClassificationThresholdMinutes { get; set; } = 5;

    /// <summary>
    /// Default alert threshold in minutes for high-priority notifications
    /// </summary>
    public int DefaultAlertThresholdMinutes { get; set; } = 15;

    /// <summary>
    /// Monitoring interval in seconds
    /// </summary>
    public int MonitoringIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum number of lines to monitor per cycle
    /// </summary>
    public int MaxLinesPerCycle { get; set; } = 50;

    /// <summary>
    /// Timeout for each monitoring cycle in seconds
    /// </summary>
    public int CycleTimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Whether to enable detailed logging for monitoring cycles
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// Maximum number of concurrent line monitoring operations
    /// </summary>
    public int MaxConcurrency { get; set; } = 10;

    /// <summary>
    /// Whether to monitor lines without active work orders
    /// </summary>
    public bool MonitorIdleLines { get; set; } = true;

    /// <summary>
    /// Hours of recent activity required to consider a line active for monitoring
    /// </summary>
    public int RecentActivityHours { get; set; } = 4;

    /// <summary>
    /// Line-specific detection configurations
    /// </summary>
    public Dictionary<string, StoppageLineConfiguration> LineConfigurations { get; set; } = new();
}

/// <summary>
/// SignalR real-time notification configuration settings
/// </summary>
public class OeeSignalRSettings
{
    /// <summary>
    /// Whether SignalR notifications are enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// SignalR hub path
    /// </summary>
    public string HubPath { get; set; } = "/stoppageHub";

    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Keep alive interval in seconds
    /// </summary>
    public int KeepAliveIntervalSeconds { get; set; } = 15;

    /// <summary>
    /// Client timeout interval in seconds
    /// </summary>
    public int ClientTimeoutIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Maximum message buffer size
    /// </summary>
    public int MaxMessageBufferSize { get; set; } = 32 * 1024; // 32KB

    /// <summary>
    /// Whether to enable detailed connection logging
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// Allowed CORS origins for SignalR connections
    /// </summary>
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Whether to enable compression for SignalR messages
    /// </summary>
    public bool EnableCompression { get; set; } = true;

    /// <summary>
    /// Maximum concurrent connections per hub
    /// </summary>
    public int MaxConcurrentConnections { get; set; } = 1000;
}

/// <summary>
/// Line-specific stoppage detection configuration
/// </summary>
public class StoppageLineConfiguration
{
    /// <summary>
    /// Equipment line identifier
    /// </summary>
    public string LineId { get; set; } = string.Empty;

    /// <summary>
    /// Detection threshold in minutes for this line
    /// </summary>
    public int DetectionThresholdMinutes { get; set; } = 5;

    /// <summary>
    /// Classification threshold in minutes for this line
    /// </summary>
    public int ClassificationThresholdMinutes { get; set; } = 5;

    /// <summary>
    /// Alert threshold in minutes for this line
    /// </summary>
    public int AlertThresholdMinutes { get; set; } = 15;

    /// <summary>
    /// Critical threshold in minutes for this line
    /// </summary>
    public int CriticalThresholdMinutes { get; set; } = 30;

    /// <summary>
    /// Whether detection is enabled for this line
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Special handling for this line (e.g., "changeover", "maintenance")
    /// </summary>
    public string? SpecialHandling { get; set; }

    /// <summary>
    /// Custom monitoring interval for this line (if different from default)
    /// </summary>
    public int? CustomMonitoringIntervalSeconds { get; set; }
}
