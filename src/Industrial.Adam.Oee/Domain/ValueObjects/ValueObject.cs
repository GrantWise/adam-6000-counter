namespace Industrial.Adam.Oee.Domain.ValueObjects;

/// <summary>
/// Base class for value objects in Domain-Driven Design
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>
    /// Get the atomic values that define equality for this value object
    /// </summary>
    /// <returns>Enumerable of atomic values</returns>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    /// <summary>
    /// Determines whether the specified value object is equal to the current value object
    /// </summary>
    /// <param name="other">Value object to compare with current value object</param>
    /// <returns>True if value objects are equal; otherwise, false</returns>
    public bool Equals(ValueObject? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        if (GetType() != other.GetType())
            return false;

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current value object
    /// </summary>
    /// <param name="obj">Object to compare with current value object</param>
    /// <returns>True if objects are equal; otherwise, false</returns>
    public override bool Equals(object? obj)
    {
        return Equals(obj as ValueObject);
    }

    /// <summary>
    /// Gets the hash code for the value object based on its equality components
    /// </summary>
    /// <returns>Hash code for the value object</returns>
    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Aggregate(1, (current, obj) =>
            {
                unchecked
                {
                    return current * 23 + (obj?.GetHashCode() ?? 0);
                }
            });
    }

    /// <summary>
    /// Equality operator for value objects
    /// </summary>
    /// <param name="left">Left value object</param>
    /// <param name="right">Right value object</param>
    /// <returns>True if value objects are equal; otherwise, false</returns>
    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Inequality operator for value objects
    /// </summary>
    /// <param name="left">Left value object</param>
    /// <param name="right">Right value object</param>
    /// <returns>True if value objects are not equal; otherwise, false</returns>
    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !Equals(left, right);
    }
}
