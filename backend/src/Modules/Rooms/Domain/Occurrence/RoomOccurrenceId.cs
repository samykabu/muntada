using Muntada.SharedKernel.Domain;

namespace Muntada.Rooms.Domain.Occurrence;

/// <summary>
/// Strongly-typed identifier for <see cref="RoomOccurrence"/> aggregates.
/// Format: <c>occ_{encoded}</c>.
/// </summary>
public sealed record RoomOccurrenceId(string Value)
{
    /// <summary>
    /// Generates a new <see cref="RoomOccurrenceId"/> with an opaque identifier.
    /// </summary>
    /// <returns>A new <see cref="RoomOccurrenceId"/>.</returns>
    public static RoomOccurrenceId New() => new(OpaqueIdGenerator.Generate("occ"));

    /// <inheritdoc />
    public override string ToString() => Value;
}
