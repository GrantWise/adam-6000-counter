namespace Industrial.Adam.Security.Domain.ValueObjects;

/// <summary>
/// Compliance metrics summary
/// </summary>
public class ComplianceMetrics
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalLoginAttempts { get; set; }
    public int FailedLoginAttempts { get; set; }
    public int AuditRecords { get; set; }
    public int SecurityViolations { get; set; }
    public double ComplianceScore { get; set; }
}