using Muntada.Rooms.Domain.Occurrence;
using Muntada.Rooms.Domain.Participant;
using StackExchange.Redis;

namespace Muntada.Rooms.Infrastructure.Cache;

/// <summary>
/// Redis hash-based cache for real-time participant presence tracking.
/// Key pattern: <c>room:{occurrenceId}:participants</c>.
/// Provides fast reads for live room participant lists without hitting SQL.
/// </summary>
public class ParticipantStateCache
{
    private readonly IConnectionMultiplexer _redis;

    /// <summary>
    /// Initializes a new instance of the <see cref="ParticipantStateCache"/> class.
    /// </summary>
    /// <param name="redis">The Redis connection multiplexer.</param>
    public ParticipantStateCache(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    /// <summary>
    /// Adds a participant to the Redis hash for a room occurrence.
    /// </summary>
    /// <param name="occurrenceId">The room occurrence identifier.</param>
    /// <param name="participantId">The participant state identifier.</param>
    /// <param name="userId">The user ID or null for guests.</param>
    /// <param name="displayName">The participant's display name.</param>
    /// <param name="role">The participant's role.</param>
    public async Task AddParticipantAsync(
        string occurrenceId,
        string participantId,
        string? userId,
        string displayName,
        ParticipantRole role)
    {
        var db = _redis.GetDatabase();
        var key = GetKey(occurrenceId);
        var value = $"{userId ?? "guest"}|{displayName}|{role}|Muted|Off|{DateTimeOffset.UtcNow:o}";
        await db.HashSetAsync(key, participantId, value);
    }

    /// <summary>
    /// Removes a participant from the Redis hash when they leave the room.
    /// </summary>
    /// <param name="occurrenceId">The room occurrence identifier.</param>
    /// <param name="participantId">The participant state identifier.</param>
    public async Task RemoveParticipantAsync(string occurrenceId, string participantId)
    {
        var db = _redis.GetDatabase();
        var key = GetKey(occurrenceId);
        await db.HashDeleteAsync(key, participantId);
    }

    /// <summary>
    /// Gets all participants currently in a room.
    /// </summary>
    /// <param name="occurrenceId">The room occurrence identifier.</param>
    /// <returns>A list of cached participant entries.</returns>
    public async Task<List<CachedParticipant>> GetParticipantsAsync(string occurrenceId)
    {
        var db = _redis.GetDatabase();
        var key = GetKey(occurrenceId);
        var entries = await db.HashGetAllAsync(key);

        var participants = new List<CachedParticipant>();
        foreach (var entry in entries)
        {
            var parts = entry.Value.ToString().Split('|');
            if (parts.Length >= 6)
            {
                participants.Add(new CachedParticipant(
                    ParticipantId: entry.Name.ToString(),
                    UserId: parts[0] == "guest" ? null : parts[0],
                    DisplayName: parts[1],
                    Role: Enum.TryParse<ParticipantRole>(parts[2], out var role) ? role : ParticipantRole.Member,
                    AudioState: Enum.TryParse<MediaState>(parts[3], out var audio) ? audio : MediaState.Muted,
                    VideoState: Enum.TryParse<MediaState>(parts[4], out var video) ? video : MediaState.Off,
                    JoinedAt: DateTimeOffset.TryParse(parts[5], out var joinedAt) ? joinedAt : DateTimeOffset.UtcNow));
            }
        }

        return participants;
    }

    /// <summary>
    /// Updates the media state (audio/video) for a specific participant.
    /// </summary>
    /// <param name="occurrenceId">The room occurrence identifier.</param>
    /// <param name="participantId">The participant state identifier.</param>
    /// <param name="audioState">The new audio state.</param>
    /// <param name="videoState">The new video state.</param>
    public async Task UpdateMediaStateAsync(
        string occurrenceId,
        string participantId,
        MediaState audioState,
        MediaState videoState)
    {
        var db = _redis.GetDatabase();
        var key = GetKey(occurrenceId);
        var existing = await db.HashGetAsync(key, participantId);

        if (existing.HasValue)
        {
            var parts = existing.ToString().Split('|');
            if (parts.Length >= 6)
            {
                parts[3] = audioState.ToString();
                parts[4] = videoState.ToString();
                await db.HashSetAsync(key, participantId, string.Join('|', parts));
            }
        }
    }

    private static string GetKey(string occurrenceId) => $"room:{occurrenceId}:participants";
}

/// <summary>
/// Represents a participant entry cached in Redis.
/// </summary>
/// <param name="ParticipantId">The participant state identifier.</param>
/// <param name="UserId">The user ID or null for guests.</param>
/// <param name="DisplayName">The participant's display name.</param>
/// <param name="Role">The participant's role.</param>
/// <param name="AudioState">Current audio state.</param>
/// <param name="VideoState">Current video state.</param>
/// <param name="JoinedAt">UTC timestamp when the participant joined.</param>
public sealed record CachedParticipant(
    string ParticipantId,
    string? UserId,
    string DisplayName,
    ParticipantRole Role,
    MediaState AudioState,
    MediaState VideoState,
    DateTimeOffset JoinedAt);
