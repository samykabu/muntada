using Muntada.SharedKernel.Domain;

namespace Muntada.Rooms.Domain.Occurrence;

/// <summary>
/// Value object representing the single designated moderator for a room occurrence.
/// Tracks assignment time and disconnect time for grace period management.
/// </summary>
public sealed class ModeratorAssignment : ValueObject
{
    /// <summary>Gets the user identifier of the moderator.</summary>
    public string UserId { get; private set; } = default!;

    /// <summary>Gets the UTC timestamp when the moderator was assigned.</summary>
    public DateTimeOffset AssignedAt { get; private set; }

    /// <summary>Gets the UTC timestamp when the moderator disconnected, or null if still connected.</summary>
    public DateTimeOffset? DisconnectedAt { get; private set; }

    private ModeratorAssignment() { }

    /// <summary>
    /// Creates a new moderator assignment.
    /// </summary>
    /// <param name="userId">The user identifier of the moderator.</param>
    /// <returns>A new <see cref="ModeratorAssignment"/>.</returns>
    public static ModeratorAssignment Create(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new SharedKernel.Domain.Exceptions.ValidationException(
                "Validation", "Moderator user ID is required.");

        return new ModeratorAssignment
        {
            UserId = userId,
            AssignedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Records the moderator's disconnection time.
    /// </summary>
    public void RecordDisconnect()
    {
        DisconnectedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Clears the disconnection time when the moderator reconnects.
    /// </summary>
    public void RecordReconnect()
    {
        DisconnectedAt = null;
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return UserId;
        yield return AssignedAt;
        yield return DisconnectedAt;
    }
}
