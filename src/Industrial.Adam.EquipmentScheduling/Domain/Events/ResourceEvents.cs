using Industrial.Adam.EquipmentScheduling.Domain.Enums;
using Industrial.Adam.EquipmentScheduling.Domain.Interfaces;

namespace Industrial.Adam.EquipmentScheduling.Domain.Events;

/// <summary>
/// Event raised when a new resource is created
/// </summary>
public sealed record ResourceCreatedEvent(
    long ResourceId,
    string Name,
    string Code,
    ResourceType Type) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

/// <summary>
/// Event raised when a resource is updated
/// </summary>
public sealed record ResourceUpdatedEvent(
    long ResourceId,
    string Name,
    bool RequiresScheduling) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

/// <summary>
/// Event raised when a resource hierarchy changes
/// </summary>
public sealed record ResourceHierarchyChangedEvent(
    long ResourceId,
    long? ParentId,
    string? HierarchyPath) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

/// <summary>
/// Event raised when a resource is deactivated
/// </summary>
public sealed record ResourceDeactivatedEvent(
    long ResourceId,
    string Name) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

/// <summary>
/// Event raised when a resource is activated
/// </summary>
public sealed record ResourceActivatedEvent(
    long ResourceId,
    string Name) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
