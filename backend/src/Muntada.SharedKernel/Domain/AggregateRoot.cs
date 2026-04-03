namespace Muntada.SharedKernel.Domain;

/// <summary>
/// Base class for aggregate roots. Extends <see cref="Entity{TId}"/>
/// with optimistic concurrency (Version), timestamps (CreatedAt, UpdatedAt),
/// and domain event tracking.
/// </summary>
/// <typeparam name="TId">The type of the aggregate's unique identifier.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>
    /// Gets the version number for optimistic concurrency control.
    /// Incremented on each successful persist operation.
    /// </summary>
    public int Version { get; protected set; }

    /// <summary>
    /// Gets the UTC timestamp when this aggregate was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; protected set; }

    /// <summary>
    /// Gets the UTC timestamp when this aggregate was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; protected set; }

    /// <summary>
    /// Gets the read-only collection of pending domain events
    /// that have not yet been dispatched.
    /// </summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Adds a domain event to the pending events collection.
    /// Events are dispatched after the aggregate is persisted.
    /// </summary>
    /// <param name="domainEvent">The domain event to add.</param>
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clears all pending domain events. Called after events
    /// have been successfully dispatched.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    /// <summary>
    /// Increments the version number for optimistic concurrency
    /// and updates the <see cref="UpdatedAt"/> timestamp.
    /// </summary>
    public void IncrementVersion()
    {
        Version++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
