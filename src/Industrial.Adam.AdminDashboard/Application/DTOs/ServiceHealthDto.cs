using Industrial.Adam.AdminDashboard.Domain.Enums;

namespace Industrial.Adam.AdminDashboard.Application.DTOs;

/// <summary>
/// DTO for service health information
/// </summary>
public class ServiceHealthDto
{
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
    /// Gets or sets the timestamp
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the error message
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets additional data
    /// </summary>
    public Dictionary<string, object>? AdditionalData { get; set; }
}