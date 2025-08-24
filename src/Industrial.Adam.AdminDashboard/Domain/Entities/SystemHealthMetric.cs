using Industrial.Adam.AdminDashboard.Domain.Enums;

namespace Industrial.Adam.AdminDashboard.Domain.Entities;

/// <summary>
/// Represents a system health metric entry with 21 CFR Part 11 compliant audit trail
/// </summary>
public class SystemHealthMetric : AuditableEntity
{
    /// <summary>
    /// Gets or sets the unique identifier
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the service name
    /// </summary>
    public required string ServiceName { get; set; }

    /// <summary>
    /// Gets or sets the service status
    /// </summary>
    public ServiceStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the response time in milliseconds
    /// </summary>
    public int ResponseTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the error count
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this metric was recorded
    /// </summary>
    public DateTime RecordedAt { get; set; }

    /// <summary>
    /// Gets or sets additional metric data as JSON
    /// </summary>
    public string? AdditionalData { get; set; }
}