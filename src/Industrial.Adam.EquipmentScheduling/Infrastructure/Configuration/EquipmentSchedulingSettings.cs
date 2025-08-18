namespace Industrial.Adam.EquipmentScheduling.Infrastructure.Configuration;

/// <summary>
/// Configuration settings for Equipment Scheduling system
/// </summary>
public sealed class EquipmentSchedulingSettings
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "EquipmentScheduling";

    /// <summary>
    /// Database connection string
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Database schema name (default: equipment_scheduling)
    /// </summary>
    public string SchemaName { get; set; } = "equipment_scheduling";

    /// <summary>
    /// Default number of days to generate schedules in advance
    /// </summary>
    public int DefaultScheduleHorizonDays { get; set; } = 30;

    /// <summary>
    /// Maximum number of days for schedule generation in a single request
    /// </summary>
    public int MaxScheduleHorizonDays { get; set; } = 365;

    /// <summary>
    /// Whether to automatically generate schedules when patterns are assigned
    /// </summary>
    public bool AutoGenerateSchedules { get; set; } = true;

    /// <summary>
    /// Number of days to look ahead for expiring pattern assignments
    /// </summary>
    public int AssignmentExpirationWarningDays { get; set; } = 7;

    /// <summary>
    /// Cache settings
    /// </summary>
    public CacheSettings Cache { get; set; } = new();

    /// <summary>
    /// Retry policy settings
    /// </summary>
    public RetrySettings Retry { get; set; } = new();
}

/// <summary>
/// Cache configuration settings
/// </summary>
public sealed class CacheSettings
{
    /// <summary>
    /// Whether caching is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Default cache expiration in minutes
    /// </summary>
    public int DefaultExpirationMinutes { get; set; } = 15;

    /// <summary>
    /// Pattern cache expiration in minutes
    /// </summary>
    public int PatternExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Resource hierarchy cache expiration in minutes
    /// </summary>
    public int HierarchyExpirationMinutes { get; set; } = 30;
}

/// <summary>
/// Retry policy configuration settings
/// </summary>
public sealed class RetrySettings
{
    /// <summary>
    /// Whether retry policies are enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Base delay for exponential backoff in milliseconds
    /// </summary>
    public int BaseDelayMs { get; set; } = 1000;

    /// <summary>
    /// Maximum delay for exponential backoff in milliseconds
    /// </summary>
    public int MaxDelayMs { get; set; } = 30000;
}
