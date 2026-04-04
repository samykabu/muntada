using Muntada.SharedKernel.Domain;

namespace Muntada.Rooms.Domain.Invite;

/// <summary>
/// Strongly-typed identifier for <see cref="RoomInvite"/> entities.
/// Format: <c>inv_{encoded}</c>.
/// </summary>
public sealed record RoomInviteId(string Value)
{
    /// <summary>
    /// Generates a new <see cref="RoomInviteId"/> with an opaque identifier.
    /// </summary>
    /// <returns>A new <see cref="RoomInviteId"/>.</returns>
    public static RoomInviteId New() => new(OpaqueIdGenerator.Generate("inv"));

    /// <inheritdoc />
    public override string ToString() => Value;
}
