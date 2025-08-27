namespace Industrial.Adam.Security.Application.DTOs;

/// <summary>
/// Login event model for real-time notifications
/// </summary>
public class LoginEvent
{
    public string Username { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

/// <summary>
/// Audit event model for real-time notifications
/// </summary>
public class AuditEvent
{
    public int UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
}

/// <summary>
/// User activity event model for real-time notifications
/// </summary>
public class UserActivityEvent
{
    public int UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
}

/// <summary>
/// Configuration change event model for real-time notifications
/// </summary>
public class ConfigurationChangeEvent
{
    public string Key { get; set; } = string.Empty;
    public object? OldValue { get; set; }
    public object? NewValue { get; set; }
    public int UserId { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
}

/// <summary>
/// Session event model for real-time notifications
/// </summary>
public class SessionEvent
{
    public int UserId { get; set; }
    public string EventType { get; set; } = string.Empty; // Started, Ended, Terminated
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
}