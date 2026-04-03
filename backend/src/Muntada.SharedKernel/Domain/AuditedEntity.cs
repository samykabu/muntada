namespace Muntada.SharedKernel.Domain;

/// <summary>
/// Base class for audited entities. Extends <see cref="AggregateRoot{TId}"/>
/// with user-tracking fields (CreatedBy, UpdatedBy) and soft-delete support.
/// </summary>
/// <typeparam name="TId">The type of the entity's unique identifier.</typeparam>
public abstract class AuditedEntity<TId> : AggregateRoot<TId>
    where TId : notnull
{
    /// <summary>
    /// Gets or sets the opaque user ID of the creator.
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the opaque user ID of the last modifier. Null if never updated.
    /// </summary>
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Gets or sets whether this entity has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when this entity was soft-deleted. Null if not deleted.
    /// </summary>
    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>
    /// Gets or sets the opaque user ID who deleted this entity. Null if not deleted.
    /// </summary>
    public string? DeletedBy { get; set; }

    /// <summary>
    /// Marks this entity as soft-deleted with the given user ID.
    /// </summary>
    /// <param name="deletedBy">The opaque user ID performing the deletion.</param>
    public void SoftDelete(string deletedBy)
    {
        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;
    }
}
