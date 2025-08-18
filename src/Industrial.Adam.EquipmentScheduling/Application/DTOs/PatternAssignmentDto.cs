namespace Industrial.Adam.EquipmentScheduling.Application.DTOs;

/// <summary>
/// Data transfer object for pattern assignment information
/// </summary>
public sealed record PatternAssignmentDto
{
    public long Id { get; init; }
    public long ResourceId { get; init; }
    public int PatternId { get; init; }
    public DateTime EffectiveDate { get; init; }
    public DateTime? EndDate { get; init; }
    public bool IsOverride { get; init; }
    public string? AssignedBy { get; init; }
    public DateTime AssignedAt { get; init; }
    public string? Notes { get; init; }
    public ResourceDto? Resource { get; init; }
    public OperatingPatternDto? OperatingPattern { get; init; }
}

/// <summary>
/// Data transfer object for creating a pattern assignment
/// </summary>
public sealed record CreatePatternAssignmentDto
{
    public long ResourceId { get; init; }
    public int PatternId { get; init; }
    public DateTime EffectiveDate { get; init; }
    public DateTime? EndDate { get; init; }
    public bool IsOverride { get; init; }
    public string? AssignedBy { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// Data transfer object for updating a pattern assignment
/// </summary>
public sealed record UpdatePatternAssignmentDto
{
    public DateTime? EndDate { get; init; }
    public string? Notes { get; init; }
    public string? UpdatedBy { get; init; }
}

/// <summary>
/// Data transfer object for pattern assignment summary
/// </summary>
public sealed record PatternAssignmentSummaryDto
{
    public long ResourceId { get; init; }
    public required string ResourceName { get; init; }
    public required string ResourceCode { get; init; }
    public int? CurrentPatternId { get; init; }
    public string? CurrentPatternName { get; init; }
    public DateTime? CurrentEffectiveDate { get; init; }
    public DateTime? CurrentEndDate { get; init; }
    public bool HasActiveAssignment { get; init; }
    public int TotalAssignments { get; init; }
    public DateTime? LastAssignedAt { get; init; }
}
