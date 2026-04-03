using Muntada.SharedKernel.Domain;

namespace Muntada.SharedKernel.Tests.Assertions;

/// <summary>
/// Custom FluentAssertions extensions for domain objects.
/// </summary>
public static class DomainAssertions
{
    /// <summary>
    /// Asserts that the aggregate has pending domain events.
    /// </summary>
    public static void ShouldHaveDomainEvents<TId>(
        this AggregateRoot<TId> aggregate, int expectedCount)
        where TId : notnull
    {
        aggregate.DomainEvents.Should().HaveCount(expectedCount,
            $"aggregate should have {expectedCount} pending domain event(s)");
    }

    /// <summary>
    /// Asserts that the aggregate has no pending domain events.
    /// </summary>
    public static void ShouldHaveNoDomainEvents<TId>(
        this AggregateRoot<TId> aggregate)
        where TId : notnull
    {
        aggregate.DomainEvents.Should().BeEmpty(
            "aggregate should have no pending domain events");
    }
}
