namespace Industrial.Adam.EquipmentScheduling.Domain.Interfaces;

/// <summary>
/// Base interface for all domain events
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the unique identifier for this domain event
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the date and time when this event occurred
    /// </summary>
    public DateTime OccurredAt { get; }
}
