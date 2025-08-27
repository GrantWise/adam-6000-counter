namespace Industrial.Adam.Security.Domain.Entities;

/// <summary>
/// Login attempt record for security audit
/// </summary>
public class LoginAttempt
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset AttemptedAt { get; set; }
}