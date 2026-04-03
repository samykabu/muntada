namespace Muntada.SharedKernel.Domain;

/// <summary>
/// Marker interface for domain events raised by aggregates.
/// Domain events represent something that happened in the domain
/// that other parts of the system may need to react to.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the unique identifier for this event instance.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Gets the UTC timestamp when this event occurred.
    /// </summary>
    DateTimeOffset OccurredAt { get; }
}
