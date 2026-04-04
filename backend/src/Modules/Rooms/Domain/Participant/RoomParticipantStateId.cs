using Muntada.SharedKernel.Domain;

namespace Muntada.Rooms.Domain.Participant;

/// <summary>
/// Strongly-typed identifier for <see cref="RoomParticipantState"/> entities.
/// Format: <c>rps_{encoded}</c>.
/// </summary>
public sealed record RoomParticipantStateId(string Value)
{
    /// <summary>
    /// Generates a new <see cref="RoomParticipantStateId"/> with an opaque identifier.
    /// </summary>
    /// <returns>A new <see cref="RoomParticipantStateId"/>.</returns>
    public static RoomParticipantStateId New() => new(OpaqueIdGenerator.Generate("rps"));

    /// <inheritdoc />
    public override string ToString() => Value;
}
