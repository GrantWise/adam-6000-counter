namespace Industrial.Adam.EquipmentScheduling.Domain.Interfaces;

/// <summary>
/// Marker interface to identify aggregate root entities in DDD
/// </summary>
public interface IAggregateRoot
{
    /// <summary>
    /// Gets the domain events that have been raised by this aggregate root
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    /// <summary>
    /// Clears all domain events from this aggregate root
    /// </summary>
    public void ClearDomainEvents();

    /// <summary>
    /// Adds a domain event to be published
    /// </summary>
    /// <param name="domainEvent">The domain event to add</param>
    public void AddDomainEvent(IDomainEvent domainEvent);
}
