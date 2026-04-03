namespace Muntada.SharedKernel.Domain;

/// <summary>
/// Represents an integration event that crosses module boundaries.
/// Published to RabbitMQ via MassTransit for async cross-module communication.
/// Extends <see cref="IDomainEvent"/> with aggregate metadata for routing.
/// </summary>
public interface IIntegrationEvent : IDomainEvent
{
    /// <summary>
    /// Gets the opaque identifier of the source aggregate that raised this event.
    /// </summary>
    string AggregateId { get; }

    /// <summary>
    /// Gets the type name of the source aggregate.
    /// </summary>
    string AggregateType { get; }

    /// <summary>
    /// Gets the schema version of this event for backward compatibility.
    /// </summary>
    int Version { get; }
}
