using Industrial.Adam.Security.Domain.Entities;
using Industrial.Adam.Security.Domain.ValueObjects;

namespace Industrial.Adam.Security.Application.DTOs;

/// <summary>
/// Response model for compliance reporting
/// </summary>
public class ComplianceReport
{
    public string ReportType { get; set; } = string.Empty;
    public DateTimeOffset GeneratedAt { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public ComplianceMetrics Metrics { get; set; } = new();
    public List<ComplianceViolation> Violations { get; set; } = new();
}