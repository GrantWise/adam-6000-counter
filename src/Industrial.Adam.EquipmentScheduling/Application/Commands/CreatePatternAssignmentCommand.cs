using Industrial.Adam.EquipmentScheduling.Application.DTOs;
using MediatR;

namespace Industrial.Adam.EquipmentScheduling.Application.Commands;

/// <summary>
/// Command to create a pattern assignment
/// </summary>
public sealed record CreatePatternAssignmentCommand(
    long ResourceId,
    int PatternId,
    DateTime EffectiveDate,
    DateTime? EndDate = null,
    bool IsOverride = false,
    string? AssignedBy = null,
    string? Notes = null) : IRequest<PatternAssignmentDto>;

/// <summary>
/// Command to update a pattern assignment
/// </summary>
public sealed record UpdatePatternAssignmentCommand(
    long Id,
    DateTime? EndDate = null,
    string? Notes = null,
    string? UpdatedBy = null) : IRequest<PatternAssignmentDto>;

/// <summary>
/// Command to terminate a pattern assignment
/// </summary>
public sealed record TerminatePatternAssignmentCommand(
    long AssignmentId,
    string? TerminatedBy = null) : IRequest<Unit>;
