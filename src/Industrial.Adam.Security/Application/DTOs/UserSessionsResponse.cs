using Industrial.Adam.Security.Domain.Entities;

namespace Industrial.Adam.Security.Application.DTOs;

/// <summary>
/// Response model for user sessions query
/// </summary>
public class UserSessionsResponse
{
    public List<UserSession> ActiveSessions { get; set; } = new();
    public int TotalCount { get; set; }
    public DateTimeOffset LastUpdated { get; set; }
}