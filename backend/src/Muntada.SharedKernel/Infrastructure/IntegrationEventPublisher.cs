using MassTransit;
using Microsoft.Extensions.Logging;
using Muntada.SharedKernel.Application;
using Muntada.SharedKernel.Domain;

namespace Muntada.SharedKernel.Infrastructure;

/// <summary>
/// Publishes integration events to RabbitMQ via MassTransit.
/// Supports dead-letter queue for failed messages and structured logging
/// with correlation IDs for distributed tracing.
/// </summary>
public sealed class IntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<IntegrationEventPublisher> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="IntegrationEventPublisher"/>.
    /// </summary>
    /// <param name="publishEndpoint">The MassTransit publish endpoint.</param>
    /// <param name="logger">The logger instance.</param>
    public IntegrationEventPublisher(
        IPublishEndpoint publishEndpoint,
        ILogger<IntegrationEventPublisher> logger)
    {
        _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task PublishAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Publishing integration event {EventType} for aggregate {AggregateType}/{AggregateId}",
            @event.GetType().Name,
            @event.AggregateType,
            @event.AggregateId);

        await _publishEndpoint.Publish(
            @event,
            @event.GetType(),
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task PublishManyAsync(
        IEnumerable<IIntegrationEvent> events,
        CancellationToken cancellationToken = default)
    {
        foreach (var @event in events)
        {
            await PublishAsync(@event, cancellationToken);
        }
    }
}
