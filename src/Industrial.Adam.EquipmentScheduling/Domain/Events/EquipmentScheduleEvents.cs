using Industrial.Adam.EquipmentScheduling.Domain.Enums;
using Industrial.Adam.EquipmentScheduling.Domain.Interfaces;

namespace Industrial.Adam.EquipmentScheduling.Domain.Events;

/// <summary>
/// Event raised when an equipment schedule is created
/// </summary>
public sealed record EquipmentScheduleCreatedEvent(
    long ScheduleId,
    long ResourceId,
    DateTime ScheduleDate,
    decimal PlannedHours,
    bool IsException) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

/// <summary>
/// Event raised when an equipment schedule is updated
/// </summary>
public sealed record EquipmentScheduleUpdatedEvent(
    long ScheduleId,
    long ResourceId,
    DateTime ScheduleDate,
    decimal PlannedHours) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

/// <summary>
/// Event raised when an equipment schedule status changes
/// </summary>
public sealed record EquipmentScheduleStatusChangedEvent(
    long ScheduleId,
    long ResourceId,
    DateTime ScheduleDate,
    ScheduleStatus OldStatus,
    ScheduleStatus NewStatus) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

/// <summary>
/// Event raised when an equipment schedule is marked as an exception
/// </summary>
public sealed record EquipmentScheduleExceptionMarkedEvent(
    long ScheduleId,
    long ResourceId,
    DateTime ScheduleDate) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

/// <summary>
/// Event raised when an equipment schedule exception is cleared
/// </summary>
public sealed record EquipmentScheduleExceptionClearedEvent(
    long ScheduleId,
    long ResourceId,
    DateTime ScheduleDate) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

/// <summary>
/// Event raised when an equipment schedule is cancelled
/// </summary>
public sealed record EquipmentScheduleCancelledEvent(
    long ScheduleId,
    long ResourceId,
    DateTime ScheduleDate,
    string? Reason) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

/// <summary>
/// Event raised when an equipment schedule is completed
/// </summary>
public sealed record EquipmentScheduleCompletedEvent(
    long ScheduleId,
    long ResourceId,
    DateTime ScheduleDate,
    decimal PlannedHours) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
