using Muntada.SharedKernel.Domain;

namespace Muntada.Rooms.Domain.Template;

/// <summary>
/// Strongly-typed identifier for <see cref="RoomTemplate"/> aggregates.
/// Format: <c>tpl_{encoded}</c>.
/// </summary>
public sealed record RoomTemplateId(string Value)
{
    /// <summary>
    /// Generates a new <see cref="RoomTemplateId"/> with an opaque identifier.
    /// </summary>
    /// <returns>A new <see cref="RoomTemplateId"/>.</returns>
    public static RoomTemplateId New() => new(OpaqueIdGenerator.Generate("tpl"));

    /// <inheritdoc />
    public override string ToString() => Value;
}
