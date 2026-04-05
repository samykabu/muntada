using Muntada.Rooms.Domain.Occurrence;

namespace Muntada.Rooms.Application.Services;

/// <summary>
/// Service for managing grace period timers on room occurrences.
/// When the moderator disconnects, a grace period countdown starts.
/// If the moderator does not reconnect within the period, the room ends automatically.
/// </summary>
public interface IGracePeriodService
{
    /// <summary>
    /// Starts a grace period timer for the specified occurrence.
    /// After <paramref name="gracePeriodSeconds"/> seconds, a grace period expiry message
    /// will be published to end the room.
    /// </summary>
    /// <param name="occurrenceId">The occurrence entering grace period.</param>
    /// <param name="gracePeriodSeconds">The number of seconds before the room auto-ends.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartGracePeriod(RoomOccurrenceId occurrenceId, int gracePeriodSeconds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels an active grace period timer for the specified occurrence.
    /// Called when the moderator reconnects before the grace period expires.
    /// </summary>
    /// <param name="occurrenceId">The occurrence exiting grace period.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CancelGracePeriod(RoomOccurrenceId occurrenceId, CancellationToken cancellationToken = default);
}
