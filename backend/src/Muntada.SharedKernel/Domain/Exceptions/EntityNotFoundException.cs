namespace Muntada.SharedKernel.Domain.Exceptions;

/// <summary>
/// Thrown when a requested entity cannot be found. Maps to HTTP 404 Not Found.
/// </summary>
public sealed class EntityNotFoundException : DomainException
{
    /// <summary>
    /// Gets the type name of the entity that was not found.
    /// </summary>
    public string EntityType { get; }

    /// <summary>
    /// Gets the identifier of the entity that was not found.
    /// </summary>
    public string EntityId { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="EntityNotFoundException"/>.
    /// </summary>
    /// <param name="entityType">The type name of the missing entity.</param>
    /// <param name="entityId">The identifier that was searched for.</param>
    public EntityNotFoundException(string entityType, string entityId)
        : base($"{entityType} with ID '{entityId}' was not found.")
    {
        EntityType = entityType;
        EntityId = entityId;
    }
}
