using Industrial.Adam.EquipmentScheduling.Domain.Enums;

namespace Industrial.Adam.EquipmentScheduling.Application.DTOs;

/// <summary>
/// Data transfer object for resource information
/// </summary>
public sealed record ResourceDto
{
    public long Id { get; init; }
    public required string Name { get; init; }
    public required string Code { get; init; }
    public ResourceType Type { get; init; }
    public long? ParentId { get; init; }
    public string? HierarchyPath { get; init; }
    public bool RequiresScheduling { get; init; }
    public bool IsActive { get; init; }
    public string? Description { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// Data transfer object for creating a resource
/// </summary>
public sealed record CreateResourceDto
{
    public required string Name { get; init; }
    public required string Code { get; init; }
    public ResourceType Type { get; init; }
    public long? ParentId { get; init; }
    public bool RequiresScheduling { get; init; }
    public string? Description { get; init; }
}

/// <summary>
/// Data transfer object for updating a resource
/// </summary>
public sealed record UpdateResourceDto
{
    public required string Name { get; init; }
    public bool RequiresScheduling { get; init; }
    public string? Description { get; init; }
}

/// <summary>
/// Data transfer object for resource hierarchy information
/// </summary>
public sealed record ResourceHierarchyDto
{
    public long Id { get; init; }
    public required string Name { get; init; }
    public required string Code { get; init; }
    public ResourceType Type { get; init; }
    public long? ParentId { get; init; }
    public int Level { get; init; }
    public List<ResourceHierarchyDto> Children { get; init; } = [];
}
