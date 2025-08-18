using System.Text.Json;
using Industrial.Adam.EquipmentScheduling.Application.DTOs;
using Industrial.Adam.EquipmentScheduling.Domain.Enums;
using MediatR;

namespace Industrial.Adam.EquipmentScheduling.Application.Commands;

/// <summary>
/// Command to create a new operating pattern
/// </summary>
public sealed record CreateOperatingPatternCommand(
    string Name,
    PatternType Type,
    int CycleDays,
    decimal WeeklyHours,
    JsonDocument Configuration,
    string? Description = null) : IRequest<OperatingPatternDto>;

/// <summary>
/// Command to update an existing operating pattern
/// </summary>
public sealed record UpdateOperatingPatternCommand(
    int Id,
    string Name,
    int CycleDays,
    decimal WeeklyHours,
    JsonDocument Configuration,
    string? Description = null) : IRequest<OperatingPatternDto>;

/// <summary>
/// Command to hide an operating pattern
/// </summary>
public sealed record HideOperatingPatternCommand(
    int PatternId) : IRequest<Unit>;

/// <summary>
/// Command to show an operating pattern
/// </summary>
public sealed record ShowOperatingPatternCommand(
    int PatternId) : IRequest<Unit>;
