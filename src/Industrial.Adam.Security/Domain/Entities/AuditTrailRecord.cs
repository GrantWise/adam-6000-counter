namespace Industrial.Adam.Security.Domain.Entities;

/// <summary>
/// Audit trail record for tracking user actions
/// </summary>
public class AuditTrailRecord
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}