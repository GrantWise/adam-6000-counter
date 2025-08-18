using Industrial.Adam.EquipmentScheduling.Domain.Enums;
using Industrial.Adam.EquipmentScheduling.Domain.Interfaces;

namespace Industrial.Adam.EquipmentScheduling.Domain.Events;

/// <summary>
/// Event raised when a new operating pattern is created
/// </summary>
public sealed record OperatingPatternCreatedEvent(
    int PatternId,
    string Name,
    PatternType Type,
    decimal WeeklyHours) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

/// <summary>
/// Event raised when an operating pattern is updated
/// </summary>
public sealed record OperatingPatternUpdatedEvent(
    int PatternId,
    string Name,
    decimal WeeklyHours,
    bool SignificantChange) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

/// <summary>
/// Event raised when an operating pattern's visibility changes
/// </summary>
public sealed record OperatingPatternVisibilityChangedEvent(
    int PatternId,
    string Name,
    bool IsVisible) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
