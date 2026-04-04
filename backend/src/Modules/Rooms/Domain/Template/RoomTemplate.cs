using Muntada.Rooms.Domain.Events;
using Muntada.SharedKernel.Domain;

namespace Muntada.Rooms.Domain.Template;

/// <summary>
/// Aggregate root representing a reusable room configuration template.
/// Templates are tenant-scoped with an immutable name for audit purposes.
/// </summary>
public class RoomTemplate : AggregateRoot<RoomTemplateId>
{
    /// <summary>Gets the tenant that owns this template.</summary>
    public string TenantId { get; private set; } = default!;

    /// <summary>Gets the template name. Immutable after creation for audit.</summary>
    public string Name { get; private set; } = default!;

    /// <summary>Gets the optional description of the template.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the room settings configured for this template.</summary>
    public RoomSettings Settings { get; private set; } = default!;

    /// <summary>Gets the identifier of the user who created this template.</summary>
    public string CreatedBy { get; private set; } = default!;

    private RoomTemplate() { }

    /// <summary>
    /// Creates a new room template with the specified configuration.
    /// </summary>
    /// <param name="tenantId">The owning tenant's identifier.</param>
    /// <param name="name">The template name (3-100 chars, unique per tenant).</param>
    /// <param name="description">Optional description (max 500 chars).</param>
    /// <param name="settings">The room settings for this template.</param>
    /// <param name="createdBy">The user creating the template.</param>
    /// <returns>A new <see cref="RoomTemplate"/> instance.</returns>
    public static RoomTemplate Create(
        string tenantId,
        string name,
        string? description,
        RoomSettings settings,
        string createdBy)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length < 3 || name.Length > 100)
            throw new SharedKernel.Domain.Exceptions.ValidationException(
                "Validation", "Template name must be between 3 and 100 characters.");

        if (description is { Length: > 500 })
            throw new SharedKernel.Domain.Exceptions.ValidationException(
                "Validation", "Template description must not exceed 500 characters.");

        var template = new RoomTemplate
        {
            Id = RoomTemplateId.New(),
            TenantId = tenantId,
            Name = name,
            Description = description,
            Settings = settings,
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        template.AddDomainEvent(new RoomStatusChangedDomainEvent(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            template.Id.Value,
            "None",
            "Created"));

        return template;
    }

    /// <summary>
    /// Updates the template settings. The name cannot be changed.
    /// </summary>
    /// <param name="description">Updated description (null to clear).</param>
    /// <param name="settings">Updated room settings.</param>
    public void Update(string? description, RoomSettings settings)
    {
        if (description is { Length: > 500 })
            throw new SharedKernel.Domain.Exceptions.ValidationException(
                "Validation", "Template description must not exceed 500 characters.");

        Description = description;
        Settings = settings;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
