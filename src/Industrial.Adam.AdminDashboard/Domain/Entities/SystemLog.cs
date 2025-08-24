using LogLevel = Industrial.Adam.AdminDashboard.Domain.Enums.LogLevel;

namespace Industrial.Adam.AdminDashboard.Domain.Entities;

/// <summary>
/// Represents a system log entry with 21 CFR Part 11 compliant audit trail
/// </summary>
public class SystemLog : AuditableEntity
{
    /// <summary>
    /// Gets or sets the unique identifier
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the service name that generated this log
    /// </summary>
    public required string ServiceName { get; set; }

    /// <summary>
    /// Gets or sets the log level
    /// </summary>
    public LogLevel LogLevel { get; set; }

    /// <summary>
    /// Gets or sets the log message
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Gets or sets the exception information (if any)
    /// </summary>
    public string? Exception { get; set; }

    /// <summary>
    /// Gets or sets additional properties as JSON
    /// </summary>
    public string? Properties { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID for request tracing
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the user context (if available)
    /// </summary>
    public string? UserContext { get; set; }
}