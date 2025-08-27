using System.ComponentModel.DataAnnotations;

namespace Industrial.Adam.Security.Application.DTOs;

/// <summary>
/// Request model for locking/unlocking user account
/// </summary>
public class LockUserRequest
{
    [Required]
    public string Reason { get; set; } = string.Empty;
    
    public DateTimeOffset? LockUntil { get; set; }
}