namespace Industrial.Adam.EquipmentScheduling.Domain.ValueObjects;

/// <summary>
/// Base entity class with strong typing for Id
/// </summary>
/// <typeparam name="TId">The type of the entity identifier</typeparam>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    /// <summary>
    /// Gets the unique identifier for this entity
    /// </summary>
    public TId Id { get; protected set; } = default!;

    /// <summary>
    /// Gets the date and time when this entity was created
    /// </summary>
    public DateTime CreatedAt { get; protected set; }

    /// <summary>
    /// Gets the date and time when this entity was last updated
    /// </summary>
    public DateTime UpdatedAt { get; protected set; }

    protected Entity()
    {
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    protected Entity(TId id) : this()
    {
        Id = id;
    }

    /// <summary>
    /// Marks the entity as updated by setting the UpdatedAt timestamp
    /// </summary>
    protected void MarkAsUpdated()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    public override bool Equals(object? obj)
    {
        return obj is Entity<TId> entity && Equals(entity);
    }

    public bool Equals(Entity<TId>? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (GetType() != other.GetType())
            return false;

        return Id.Equals(other.Id);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(GetType(), Id);
    }

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        return left?.Equals(right) ?? right is null;
    }

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !(left == right);
    }
}
