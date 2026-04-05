using Muntada.Rooms.Domain.Occurrence;
using Muntada.SharedKernel.Domain;

namespace Muntada.Rooms.Domain.Participant;

/// <summary>
/// Entity tracking a participant's state within a room occurrence.
/// Includes join/leave times, audio/video status, and role.
/// </summary>
public class RoomParticipantState : Entity<RoomParticipantStateId>
{
    /// <summary>Gets the room occurrence this participant belongs to.</summary>
    public RoomOccurrenceId RoomOccurrenceId { get; private set; } = default!;

    /// <summary>Gets the user ID, or null for guest participants.</summary>
    public string? UserId { get; private set; }

    /// <summary>Gets the participant's display name.</summary>
    public string DisplayName { get; private set; } = default!;

    /// <summary>Gets the participant's role in the room.</summary>
    public ParticipantRole Role { get; private set; }

    /// <summary>Gets the UTC timestamp when the participant joined.</summary>
    public DateTimeOffset JoinedAt { get; private set; }

    /// <summary>Gets the UTC timestamp when the participant left, or null if still in room.</summary>
    public DateTimeOffset? LeftAt { get; private set; }

    /// <summary>Gets the current audio state.</summary>
    public MediaState AudioState { get; private set; }

    /// <summary>Gets the current video state.</summary>
    public MediaState VideoState { get; private set; }

    /// <summary>Gets the LiveKit participant identifier for webhook correlation.</summary>
    public string? LiveKitParticipantId { get; private set; }

    private RoomParticipantState() { }

    /// <summary>
    /// Creates a new participant state record.
    /// </summary>
    public static RoomParticipantState Create(
        RoomOccurrenceId occurrenceId,
        string? userId,
        string displayName,
        ParticipantRole role,
        string? liveKitParticipantId = null)
    {
        if (string.IsNullOrWhiteSpace(displayName) || displayName.Length > 100)
            throw new SharedKernel.Domain.Exceptions.ValidationException(
                "Validation", "Display name is required and must not exceed 100 characters.");

        return new RoomParticipantState
        {
            Id = RoomParticipantStateId.New(),
            RoomOccurrenceId = occurrenceId,
            UserId = userId,
            DisplayName = displayName,
            Role = role,
            JoinedAt = DateTimeOffset.UtcNow,
            AudioState = MediaState.Muted,
            VideoState = MediaState.Off,
            LiveKitParticipantId = liveKitParticipantId
        };
    }

    /// <summary>Records that the participant left the room.</summary>
    public void RecordLeave()
    {
        LeftAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Updates the participant's audio state.</summary>
    public void UpdateAudioState(MediaState state)
    {
        AudioState = state;
    }

    /// <summary>Updates the participant's video state.</summary>
    public void UpdateVideoState(MediaState state)
    {
        VideoState = state;
    }

    /// <summary>Gets the duration in seconds the participant was in the room.</summary>
    public long GetDwellTimeSeconds()
    {
        var end = LeftAt ?? DateTimeOffset.UtcNow;
        return (long)(end - JoinedAt).TotalSeconds;
    }
}
