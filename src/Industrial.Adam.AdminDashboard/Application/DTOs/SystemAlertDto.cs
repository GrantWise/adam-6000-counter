using Industrial.Adam.AdminDashboard.Domain.Enums;

namespace Industrial.Adam.AdminDashboard.Application.DTOs;

/// <summary>
/// DTO for system alerts
/// </summary>
public class SystemAlertDto
{
    /// <summary>
    /// Gets or sets the alert ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the alert type
    /// </summary>
    public required string AlertType { get; set; }

    /// <summary>
    /// Gets or sets the severity
    /// </summary>
    public AlertSeverity Severity { get; set; }

    /// <summary>
    /// Gets or sets the message
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Gets or sets whether acknowledged
    /// </summary>
    public bool Acknowledged { get; set; }

    /// <summary>
    /// Gets or sets who acknowledged the alert
    /// </summary>
    public string? AcknowledgedBy { get; set; }

    /// <summary>
    /// Gets or sets when acknowledged
    /// </summary>
    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>
    /// Gets or sets when created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the service name
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Gets or sets additional data
    /// </summary>
    public Dictionary<string, object>? AdditionalData { get; set; }
}