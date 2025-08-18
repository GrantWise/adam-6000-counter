using Industrial.Adam.EquipmentScheduling.Application.DTOs;
using Industrial.Adam.EquipmentScheduling.Domain.Enums;
using MediatR;

namespace Industrial.Adam.EquipmentScheduling.Application.Queries;

/// <summary>
/// Query to get an operating pattern by ID
/// </summary>
public sealed record GetOperatingPatternByIdQuery(
    int PatternId) : IRequest<OperatingPatternDto?>;

/// <summary>
/// Query to get an operating pattern by name
/// </summary>
public sealed record GetOperatingPatternByNameQuery(
    string Name) : IRequest<OperatingPatternDto?>;

/// <summary>
/// Query to get operating patterns by type
/// </summary>
public sealed record GetOperatingPatternsByTypeQuery(
    PatternType Type,
    bool VisibleOnly = true) : IRequest<IEnumerable<OperatingPatternDto>>;

/// <summary>
/// Query to get all visible operating patterns
/// </summary>
public sealed record GetVisibleOperatingPatternsQuery() : IRequest<IEnumerable<OperatingPatternDto>>;

/// <summary>
/// Query to get operating patterns by weekly hours range
/// </summary>
public sealed record GetOperatingPatternsByHoursRangeQuery(
    decimal MinHours,
    decimal MaxHours,
    bool VisibleOnly = true) : IRequest<IEnumerable<OperatingPatternDto>>;

/// <summary>
/// Query to get pattern availability information
/// </summary>
public sealed record GetPatternAvailabilityQuery(
    int PatternId) : IRequest<PatternAvailabilityDto?>;

/// <summary>
/// Query to get all pattern availabilities
/// </summary>
public sealed record GetAllPatternAvailabilitiesQuery(
    bool VisibleOnly = true) : IRequest<IEnumerable<PatternAvailabilityDto>>;
