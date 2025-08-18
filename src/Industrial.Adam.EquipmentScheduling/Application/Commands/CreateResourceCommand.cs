using Industrial.Adam.EquipmentScheduling.Application.DTOs;
using Industrial.Adam.EquipmentScheduling.Domain.Enums;
using MediatR;

namespace Industrial.Adam.EquipmentScheduling.Application.Commands;

/// <summary>
/// Command to create a new resource
/// </summary>
public sealed record CreateResourceCommand(
    string Name,
    string Code,
    ResourceType Type,
    long? ParentId = null,
    bool RequiresScheduling = false,
    string? Description = null) : IRequest<ResourceDto>;

/// <summary>
/// Command to update an existing resource
/// </summary>
public sealed record UpdateResourceCommand(
    long Id,
    string Name,
    bool RequiresScheduling,
    string? Description = null) : IRequest<ResourceDto>;

/// <summary>
/// Command to set resource parent hierarchy
/// </summary>
public sealed record SetResourceParentCommand(
    long ResourceId,
    long? ParentId) : IRequest<Unit>;

/// <summary>
/// Command to activate a resource
/// </summary>
public sealed record ActivateResourceCommand(
    long ResourceId) : IRequest<Unit>;

/// <summary>
/// Command to deactivate a resource
/// </summary>
public sealed record DeactivateResourceCommand(
    long ResourceId) : IRequest<Unit>;
