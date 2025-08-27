using Industrial.Adam.Security.Domain.Entities;

namespace Industrial.Adam.Security.Application.DTOs;

/// <summary>
/// Response model for user activity history
/// </summary>
public class UserActivityResponse
{
    public List<UserActivity> Activities { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasNext { get; set; }
}