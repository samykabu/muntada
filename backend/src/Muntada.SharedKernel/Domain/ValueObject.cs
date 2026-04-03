namespace Muntada.SharedKernel.Domain;

/// <summary>
/// Base class for value objects. Value objects are immutable and
/// compared by their structural equality (field values) rather
/// than by reference or identity.
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>
    /// Returns the components used for equality comparison.
    /// Derived classes must return all properties that define
    /// the value object's identity.
    /// </summary>
    /// <returns>An enumerable of equality components.</returns>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    /// <inheritdoc />
    public bool Equals(ValueObject? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;

        return GetEqualityComponents()
            .SequenceEqual(other.GetEqualityComponents());
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is ValueObject valueObject && Equals(valueObject);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Aggregate(0, (hash, component) =>
                HashCode.Combine(hash, component?.GetHashCode() ?? 0));
    }

    /// <summary>
    /// Equality operator for value objects.
    /// </summary>
    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Inequality operator for value objects.
    /// </summary>
    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !Equals(left, right);
    }
}
