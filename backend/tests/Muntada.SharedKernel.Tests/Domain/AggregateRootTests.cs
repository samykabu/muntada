using Muntada.SharedKernel.Domain;

namespace Muntada.SharedKernel.Tests.Domain;

public class AggregateRootTests
{
    private class TestEvent : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
    }

    private class TestAggregate : AggregateRoot<Guid>
    {
        public TestAggregate(Guid id)
        {
            Id = id;
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = CreatedAt;
        }

        public void RaiseTestEvent() => AddDomainEvent(new TestEvent());
    }

    [Fact]
    public void New_aggregate_should_have_version_zero()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());

        aggregate.Version.Should().Be(0);
    }

    [Fact]
    public void IncrementVersion_should_increase_version_by_one()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());

        aggregate.IncrementVersion();

        aggregate.Version.Should().Be(1);
    }

    [Fact]
    public void IncrementVersion_should_update_UpdatedAt()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        var initialUpdatedAt = aggregate.UpdatedAt;

        aggregate.IncrementVersion();

        aggregate.UpdatedAt.Should().BeOnOrAfter(initialUpdatedAt);
    }

    [Fact]
    public void AddDomainEvent_should_add_event_to_collection()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());

        aggregate.RaiseTestEvent();

        aggregate.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public void ClearDomainEvents_should_empty_collection()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        aggregate.RaiseTestEvent();
        aggregate.RaiseTestEvent();

        aggregate.ClearDomainEvents();

        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Multiple_events_should_accumulate()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());

        aggregate.RaiseTestEvent();
        aggregate.RaiseTestEvent();
        aggregate.RaiseTestEvent();

        aggregate.DomainEvents.Should().HaveCount(3);
    }
}
