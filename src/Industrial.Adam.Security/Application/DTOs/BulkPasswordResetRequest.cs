using System.ComponentModel.DataAnnotations;

namespace Industrial.Adam.Security.Application.DTOs;

/// <summary>
/// Request model for bulk password reset
/// </summary>
public class BulkPasswordResetRequest
{
    [Required]
    public string[] UserIds { get; set; } = [];
    
    public bool ForcePasswordChange { get; set; } = true;
    
    public bool NotifyUsers { get; set; } = true;
}