namespace Industrial.Adam.Security.Domain.Entities;

/// <summary>
/// User session information for session tracking
/// </summary>
public class UserSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset LastActivity { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
}