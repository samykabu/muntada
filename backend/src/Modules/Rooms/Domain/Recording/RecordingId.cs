using Muntada.SharedKernel.Domain;

namespace Muntada.Rooms.Domain.Recording;

/// <summary>
/// Strongly-typed identifier for <see cref="Recording"/> aggregates.
/// Format: <c>rec_{encoded}</c>.
/// </summary>
public sealed record RecordingId(string Value)
{
    /// <summary>
    /// Generates a new <see cref="RecordingId"/> with an opaque identifier.
    /// </summary>
    /// <returns>A new <see cref="RecordingId"/>.</returns>
    public static RecordingId New() => new(OpaqueIdGenerator.Generate("rec"));

    /// <inheritdoc />
    public override string ToString() => Value;
}
