using Microsoft.AspNetCore.SignalR;

namespace Muntada.Rooms.Api.Hubs;

/// <summary>
/// SignalR hub for real-time room communication.
/// Clients subscribe to room-specific groups to receive live updates about
/// participant changes, room status transitions, and recording events.
/// </summary>
public class RoomHub : Hub
{
    /// <summary>
    /// Adds the calling connection to a room-specific SignalR group
    /// to receive real-time updates for that room occurrence.
    /// </summary>
    /// <param name="occurrenceId">The room occurrence identifier to subscribe to.</param>
    public async Task JoinRoomGroup(string occurrenceId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(occurrenceId));
    }

    /// <summary>
    /// Removes the calling connection from a room-specific SignalR group.
    /// </summary>
    /// <param name="occurrenceId">The room occurrence identifier to unsubscribe from.</param>
    public async Task LeaveRoomGroup(string occurrenceId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetGroupName(occurrenceId));
    }

    /// <summary>
    /// Broadcasts a ParticipantJoined event to all connections in the room group.
    /// </summary>
    /// <param name="hubContext">The hub context for broadcasting.</param>
    /// <param name="occurrenceId">The room occurrence identifier.</param>
    /// <param name="participantId">The participant state identifier.</param>
    /// <param name="displayName">The participant's display name.</param>
    /// <param name="role">The participant's role.</param>
    public static async Task BroadcastParticipantJoined(
        IHubContext<RoomHub> hubContext,
        string occurrenceId,
        string participantId,
        string displayName,
        string role)
    {
        await hubContext.Clients.Group(GetGroupName(occurrenceId))
            .SendAsync("ParticipantJoined", new { participantId, displayName, role });
    }

    /// <summary>
    /// Broadcasts a ParticipantLeft event to all connections in the room group.
    /// </summary>
    /// <param name="hubContext">The hub context for broadcasting.</param>
    /// <param name="occurrenceId">The room occurrence identifier.</param>
    /// <param name="participantId">The participant state identifier.</param>
    public static async Task BroadcastParticipantLeft(
        IHubContext<RoomHub> hubContext,
        string occurrenceId,
        string participantId)
    {
        await hubContext.Clients.Group(GetGroupName(occurrenceId))
            .SendAsync("ParticipantLeft", new { participantId });
    }

    /// <summary>
    /// Broadcasts a ParticipantMediaChanged event to all connections in the room group.
    /// </summary>
    /// <param name="hubContext">The hub context for broadcasting.</param>
    /// <param name="occurrenceId">The room occurrence identifier.</param>
    /// <param name="participantId">The participant state identifier.</param>
    /// <param name="audioState">The current audio state.</param>
    /// <param name="videoState">The current video state.</param>
    public static async Task BroadcastParticipantMediaChanged(
        IHubContext<RoomHub> hubContext,
        string occurrenceId,
        string participantId,
        string audioState,
        string videoState)
    {
        await hubContext.Clients.Group(GetGroupName(occurrenceId))
            .SendAsync("ParticipantMediaChanged", new { participantId, audioState, videoState });
    }

    /// <summary>
    /// Broadcasts a RoomStatusChanged event to all connections in the room group.
    /// </summary>
    /// <param name="hubContext">The hub context for broadcasting.</param>
    /// <param name="occurrenceId">The room occurrence identifier.</param>
    /// <param name="previousStatus">The previous room status.</param>
    /// <param name="newStatus">The new room status.</param>
    public static async Task BroadcastRoomStatusChanged(
        IHubContext<RoomHub> hubContext,
        string occurrenceId,
        string previousStatus,
        string newStatus)
    {
        await hubContext.Clients.Group(GetGroupName(occurrenceId))
            .SendAsync("RoomStatusChanged", new { occurrenceId, previousStatus, newStatus });
    }

    /// <summary>
    /// Broadcasts a ModeratorChanged event to all connections in the room group.
    /// </summary>
    /// <param name="hubContext">The hub context for broadcasting.</param>
    /// <param name="occurrenceId">The room occurrence identifier.</param>
    /// <param name="newModeratorUserId">The user ID of the new moderator.</param>
    public static async Task BroadcastModeratorChanged(
        IHubContext<RoomHub> hubContext,
        string occurrenceId,
        string newModeratorUserId)
    {
        await hubContext.Clients.Group(GetGroupName(occurrenceId))
            .SendAsync("ModeratorChanged", new { occurrenceId, newModeratorUserId });
    }

    /// <summary>
    /// Broadcasts a RecordingStatusChanged event to all connections in the room group.
    /// </summary>
    /// <param name="hubContext">The hub context for broadcasting.</param>
    /// <param name="occurrenceId">The room occurrence identifier.</param>
    /// <param name="recordingId">The recording identifier.</param>
    /// <param name="status">The recording status.</param>
    public static async Task BroadcastRecordingStatusChanged(
        IHubContext<RoomHub> hubContext,
        string occurrenceId,
        string recordingId,
        string status)
    {
        await hubContext.Clients.Group(GetGroupName(occurrenceId))
            .SendAsync("RecordingStatusChanged", new { occurrenceId, recordingId, status });
    }

    private static string GetGroupName(string occurrenceId) => $"room-{occurrenceId}";
}
