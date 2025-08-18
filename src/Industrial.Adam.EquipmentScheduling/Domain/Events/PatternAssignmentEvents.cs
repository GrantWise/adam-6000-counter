using Industrial.Adam.EquipmentScheduling.Domain.Interfaces;

namespace Industrial.Adam.EquipmentScheduling.Domain.Events;

/// <summary>
/// Event raised when a pattern is assigned to a resource
/// </summary>
public sealed record PatternAssignmentCreatedEvent(
    long AssignmentId,
    long ResourceId,
    int PatternId,
    DateTime EffectiveDate,
    bool IsOverride) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

/// <summary>
/// Event raised when a pattern assignment is updated
/// </summary>
public sealed record PatternAssignmentUpdatedEvent(
    long AssignmentId,
    long ResourceId,
    int PatternId,
    DateTime? EndDate) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

/// <summary>
/// Event raised when a pattern assignment is terminated
/// </summary>
public sealed record PatternAssignmentTerminatedEvent(
    long AssignmentId,
    long ResourceId,
    int PatternId,
    DateTime TerminationDate) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
