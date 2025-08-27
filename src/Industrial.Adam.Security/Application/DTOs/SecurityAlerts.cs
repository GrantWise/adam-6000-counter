namespace Industrial.Adam.Security.Application.DTOs;

/// <summary>
/// Security alert types
/// </summary>
public enum SecurityAlertType
{
    ExcessiveFailedLogins,
    SuspiciousActivity,
    UnauthorizedAccess,
    DataBreach,
    SystemVulnerability
}

/// <summary>
/// Security alert severity levels
/// </summary>
public enum SecurityAlertSeverity
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Unified security alert model
/// </summary>
public class SecurityAlert
{
    public string Id { get; set; } = string.Empty;
    public SecurityAlertType Type { get; set; }
    public SecurityAlertSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsAcknowledged { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string? IpAddress { get; set; }
    public string? Username { get; set; }
    public int Count { get; set; } = 1;
}