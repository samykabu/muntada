using Muntada.SharedKernel.Domain;

namespace Muntada.Rooms.Domain.Series;

/// <summary>
/// Strongly-typed identifier for <see cref="RoomSeries"/> aggregates.
/// Format: <c>ser_{encoded}</c>.
/// </summary>
public sealed record RoomSeriesId(string Value)
{
    /// <summary>
    /// Generates a new <see cref="RoomSeriesId"/> with an opaque identifier.
    /// </summary>
    /// <returns>A new <see cref="RoomSeriesId"/>.</returns>
    public static RoomSeriesId New() => new(OpaqueIdGenerator.Generate("ser"));

    /// <inheritdoc />
    public override string ToString() => Value;
}
