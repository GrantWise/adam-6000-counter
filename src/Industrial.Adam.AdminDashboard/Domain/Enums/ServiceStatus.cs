namespace Industrial.Adam.AdminDashboard.Domain.Enums;

/// <summary>
/// Represents the health status of a service
/// </summary>
public enum ServiceStatus
{
    /// <summary>
    /// Service is operating normally
    /// </summary>
    Healthy = 0,

    /// <summary>
    /// Service is experiencing issues but still functional
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Service is not functioning properly
    /// </summary>
    Critical = 2,

    /// <summary>
    /// Service status cannot be determined
    /// </summary>
    Unknown = 3
}