using Industrial.Adam.Security.Domain.Entities;

namespace Industrial.Adam.Security.Application.DTOs;

/// <summary>
/// Response model for audit trail query
/// </summary>
public class AuditTrailResponse
{
    public List<AuditTrailRecord> Records { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasNext { get; set; }
}