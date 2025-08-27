namespace Industrial.Adam.Security.Domain.Entities;

/// <summary>
/// User activity record for tracking user actions
/// </summary>
public class UserActivity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
}