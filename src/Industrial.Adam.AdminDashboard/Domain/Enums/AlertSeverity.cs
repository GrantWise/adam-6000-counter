namespace Industrial.Adam.AdminDashboard.Domain.Enums;

/// <summary>
/// Represents the severity level of system alerts
/// </summary>
public enum AlertSeverity
{
    /// <summary>
    /// Informational alert
    /// </summary>
    Info = 0,

    /// <summary>
    /// Warning level alert
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Error level alert
    /// </summary>
    Error = 2,

    /// <summary>
    /// Critical system alert
    /// </summary>
    Critical = 3
}