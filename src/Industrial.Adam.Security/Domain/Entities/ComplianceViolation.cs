namespace Industrial.Adam.Security.Domain.Entities;

/// <summary>
/// Compliance violation record
/// </summary>
public class ComplianceViolation
{
    public string ViolationType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public string Status { get; set; } = string.Empty;
}