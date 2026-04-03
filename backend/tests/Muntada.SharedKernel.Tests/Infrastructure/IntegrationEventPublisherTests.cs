using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using Muntada.SharedKernel.Domain;
using Muntada.SharedKernel.Infrastructure;

namespace Muntada.SharedKernel.Tests.Infrastructure;

public class IntegrationEventPublisherTests
{
    private readonly Mock<IPublishEndpoint> _publishEndpoint = new();
    private readonly Mock<ILogger<IntegrationEventPublisher>> _logger = new();

    private class TestIntegrationEvent : IIntegrationEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
        public string AggregateId { get; init; } = "usr_test123";
        public string AggregateType { get; init; } = "User";
        public int Version { get; init; } = 1;
    }

    [Fact]
    public async Task PublishAsync_should_call_MassTransit_publish()
    {
        var publisher = new IntegrationEventPublisher(
            _publishEndpoint.Object, _logger.Object);
        var @event = new TestIntegrationEvent();

        await publisher.PublishAsync(@event);

        _publishEndpoint.Verify(
            p => p.Publish(
                It.IsAny<object>(),
                It.IsAny<Type>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishManyAsync_should_publish_all_events()
    {
        var publisher = new IntegrationEventPublisher(
            _publishEndpoint.Object, _logger.Object);
        var events = new[] { new TestIntegrationEvent(), new TestIntegrationEvent() };

        await publisher.PublishManyAsync(events);

        _publishEndpoint.Verify(
            p => p.Publish(
                It.IsAny<object>(),
                It.IsAny<Type>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public void Constructor_should_throw_on_null_publishEndpoint()
    {
        var act = () => new IntegrationEventPublisher(null!, _logger.Object);

        act.Should().Throw<ArgumentNullException>();
    }
}
