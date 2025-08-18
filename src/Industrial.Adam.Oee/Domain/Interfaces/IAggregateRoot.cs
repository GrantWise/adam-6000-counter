namespace Industrial.Adam.Oee.Domain.Interfaces;

/// <summary>
/// Marker interface for aggregate roots in Domain-Driven Design
/// </summary>
public interface IAggregateRoot
{
}

/// <summary>
/// Base interface for domain entities
/// </summary>
/// <typeparam name="TId">Type of the entity identifier</typeparam>
public interface IEntity<TId>
{
    /// <summary>
    /// Unique identifier for the entity
    /// </summary>
    public TId Id { get; }
}

/// <summary>
/// Base class for domain entities
/// </summary>
/// <typeparam name="TId">Type of the entity identifier</typeparam>
public abstract class Entity<TId> : IEntity<TId>, IEquatable<Entity<TId>>
    where TId : IEquatable<TId>
{
    /// <summary>
    /// Unique identifier for the entity
    /// </summary>
    public TId Id { get; protected set; } = default!;

    /// <summary>
    /// Default constructor for entity
    /// </summary>
    protected Entity() { }

    /// <summary>
    /// Constructor with identifier
    /// </summary>
    /// <param name="id">Entity identifier</param>
    protected Entity(TId id)
    {
        Id = id;
    }

    /// <summary>
    /// Determines whether the specified entity is equal to the current entity
    /// </summary>
    /// <param name="other">Entity to compare with current entity</param>
    /// <returns>True if entities are equal; otherwise, false</returns>
    public bool Equals(Entity<TId>? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return Id.Equals(other.Id);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current entity
    /// </summary>
    /// <param name="obj">Object to compare with current entity</param>
    /// <returns>True if objects are equal; otherwise, false</returns>
    public override bool Equals(object? obj)
    {
        return Equals(obj as Entity<TId>);
    }

    /// <summary>
    /// Gets the hash code for the entity based on its identifier
    /// </summary>
    /// <returns>Hash code for the entity</returns>
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    /// <summary>
    /// Equality operator for entities
    /// </summary>
    /// <param name="left">Left entity</param>
    /// <param name="right">Right entity</param>
    /// <returns>True if entities are equal; otherwise, false</returns>
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Inequality operator for entities
    /// </summary>
    /// <param name="left">Left entity</param>
    /// <param name="right">Right entity</param>
    /// <returns>True if entities are not equal; otherwise, false</returns>
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !Equals(left, right);
    }
}
