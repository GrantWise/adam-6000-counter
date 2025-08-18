namespace Industrial.Adam.EquipmentScheduling.Domain.ValueObjects;

/// <summary>
/// Base class for value objects that ensures proper equality semantics
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>
    /// Gets the atomic values that make up this value object for equality comparison
    /// </summary>
    /// <returns>An enumerable of objects representing the value object's components</returns>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((ValueObject)obj);
    }

    public bool Equals(ValueObject? other)
    {
        if (other is null)
            return false;

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Where(component => component is not null)
            .Aggregate(1, (current, component) =>
                HashCode.Combine(current, component!.GetHashCode()));
    }

    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        return left?.Equals(right) ?? right is null;
    }

    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !(left == right);
    }
}
