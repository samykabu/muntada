using Microsoft.AspNetCore.SignalR;
using Muntada.Rooms.Api.Dtos;

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
        var payload = new ParticipantJoinedPayload(participantId, displayName, role);
        await hubContext.Clients.Group(GetGroupName(occurrenceId))
            .SendAsync("ParticipantJoined", payload);
    }

    /// <summary>
    /// Broadcasts a ParticipantLeft event to all connections in the room group.
    /// </summary>
    /// <param name="hubContext">The hub context for broadcasting.</param>
    /// <param name="occurrenceId">The room occurrence identifier.</param>
    /// <param name="participantId">The participant state identifier.</param>
    /// <param name="userId">The user ID of the participant who left, or null for guests.</param>
    /// <param name="leftAt">The timestamp when the participant left.</param>
    public static async Task BroadcastParticipantLeft(
        IHubContext<RoomHub> hubContext,
        string occurrenceId,
        string participantId,
        string? userId = null,
        DateTimeOffset? leftAt = null)
    {
        var payload = new ParticipantLeftPayload(participantId, userId, leftAt ?? DateTimeOffset.UtcNow);
        await hubContext.Clients.Group(GetGroupName(occurrenceId))
            .SendAsync("ParticipantLeft", payload);
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
        var payload = new ParticipantMediaChangedPayload(participantId, audioState, videoState);
        await hubContext.Clients.Group(GetGroupName(occurrenceId))
            .SendAsync("ParticipantMediaChanged", payload);
    }

    /// <summary>
    /// Broadcasts a RoomStatusChanged event to all connections in the room group.
    /// </summary>
    /// <param name="hubContext">The hub context for broadcasting.</param>
    /// <param name="occurrenceId">The room occurrence identifier.</param>
    /// <param name="status">The new room status.</param>
    /// <param name="graceStartedAt">When the grace period started, if applicable.</param>
    /// <param name="graceExpiresAt">When the grace period expires, if applicable.</param>
    public static async Task BroadcastRoomStatusChanged(
        IHubContext<RoomHub> hubContext,
        string occurrenceId,
        string status,
        DateTimeOffset? graceStartedAt = null,
        DateTimeOffset? graceExpiresAt = null)
    {
        var payload = new RoomStatusChangedPayload(occurrenceId, status, graceStartedAt, graceExpiresAt);
        await hubContext.Clients.Group(GetGroupName(occurrenceId))
            .SendAsync("RoomStatusChanged", payload);
    }

    /// <summary>
    /// Broadcasts a ModeratorChanged event to all connections in the room group.
    /// </summary>
    /// <param name="hubContext">The hub context for broadcasting.</param>
    /// <param name="occurrenceId">The room occurrence identifier.</param>
    /// <param name="newModeratorUserId">The user ID of the new moderator.</param>
    /// <param name="newModeratorName">The display name of the new moderator.</param>
    public static async Task BroadcastModeratorChanged(
        IHubContext<RoomHub> hubContext,
        string occurrenceId,
        string newModeratorUserId,
        string newModeratorName = "")
    {
        var payload = new ModeratorChangedPayload(occurrenceId, newModeratorUserId, newModeratorName);
        await hubContext.Clients.Group(GetGroupName(occurrenceId))
            .SendAsync("ModeratorChanged", payload);
    }

    /// <summary>
    /// Broadcasts a RecordingStatusChanged event to all connections in the room group.
    /// </summary>
    /// <param name="hubContext">The hub context for broadcasting.</param>
    /// <param name="occurrenceId">The room occurrence identifier.</param>
    /// <param name="isRecording">Whether the room is currently being recorded.</param>
    public static async Task BroadcastRecordingStatusChanged(
        IHubContext<RoomHub> hubContext,
        string occurrenceId,
        bool isRecording)
    {
        var payload = new RecordingStatusChangedPayload(occurrenceId, isRecording);
        await hubContext.Clients.Group(GetGroupName(occurrenceId))
            .SendAsync("RecordingStatusChanged", payload);
    }

    private static string GetGroupName(string occurrenceId) => $"room-{occurrenceId}";
}
