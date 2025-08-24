using Industrial.Adam.AdminDashboard.Domain.Enums;

namespace Industrial.Adam.AdminDashboard.Domain.Entities;

/// <summary>
/// Represents a system alert with 21 CFR Part 11 compliant audit trail
/// </summary>
public class SystemAlert : AuditableEntity
{
    /// <summary>
    /// Gets or sets the unique identifier
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the alert type
    /// </summary>
    public required string AlertType { get; set; }

    /// <summary>
    /// Gets or sets the alert severity
    /// </summary>
    public AlertSeverity Severity { get; set; }

    /// <summary>
    /// Gets or sets the alert message
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Gets or sets whether the alert has been acknowledged
    /// </summary>
    public bool Acknowledged { get; set; }

    /// <summary>
    /// Gets or sets the user who acknowledged the alert
    /// </summary>
    public string? AcknowledgedBy { get; set; }

    /// <summary>
    /// Gets or sets when the alert was acknowledged
    /// </summary>
    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>
    /// Gets or sets the service that generated this alert
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Gets or sets additional alert data as JSON
    /// </summary>
    public string? AdditionalData { get; set; }
}