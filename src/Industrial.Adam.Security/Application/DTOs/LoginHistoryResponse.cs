using Industrial.Adam.Security.Domain.Entities;

namespace Industrial.Adam.Security.Application.DTOs;

/// <summary>
/// Response model for login history
/// </summary>
public class LoginHistoryResponse
{
    public List<LoginAttempt> LoginAttempts { get; set; } = new();
    public int TotalCount { get; set; }
    public int SuccessfulLogins { get; set; }
    public int FailedLogins { get; set; }
    public DateTimeOffset? LastSuccessfulLogin { get; set; }
}