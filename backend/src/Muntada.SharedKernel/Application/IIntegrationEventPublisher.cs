using Muntada.SharedKernel.Domain;

namespace Muntada.SharedKernel.Application;

/// <summary>
/// Publishes integration events to the message bus (RabbitMQ via MassTransit).
/// Events cross module boundaries and are consumed by other modules asynchronously.
/// </summary>
public interface IIntegrationEventPublisher
{
    /// <summary>
    /// Publishes an integration event to the message bus.
    /// </summary>
    /// <param name="event">The integration event to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes multiple integration events to the message bus.
    /// </summary>
    /// <param name="events">The integration events to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishManyAsync(IEnumerable<IIntegrationEvent> events, CancellationToken cancellationToken = default);
}
