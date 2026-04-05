using System.Text.Json;
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

    /// <summary>TTL for participant hash keys (2 hours).</summary>
    private static readonly TimeSpan KeyTtl = TimeSpan.FromHours(2);

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
        var entry = new ParticipantCacheEntry(userId, displayName, role.ToString(), "Muted", "Off", DateTimeOffset.UtcNow);
        var value = JsonSerializer.Serialize(entry);
        await db.HashSetAsync(key, participantId, value);
        await db.KeyExpireAsync(key, KeyTtl);
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
            var cached = JsonSerializer.Deserialize<ParticipantCacheEntry>(entry.Value.ToString());
            if (cached is not null)
            {
                participants.Add(new CachedParticipant(
                    ParticipantId: entry.Name.ToString(),
                    UserId: cached.UserId,
                    DisplayName: cached.DisplayName,
                    Role: Enum.TryParse<ParticipantRole>(cached.Role, out var role) ? role : ParticipantRole.Member,
                    AudioState: Enum.TryParse<MediaState>(cached.AudioState, out var audio) ? audio : MediaState.Muted,
                    VideoState: Enum.TryParse<MediaState>(cached.VideoState, out var video) ? video : MediaState.Off,
                    JoinedAt: cached.JoinedAt));
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
            var cached = JsonSerializer.Deserialize<ParticipantCacheEntry>(existing.ToString());
            if (cached is not null)
            {
                var updated = cached with { AudioState = audioState.ToString(), VideoState = videoState.ToString() };
                await db.HashSetAsync(key, participantId, JsonSerializer.Serialize(updated));
                await db.KeyExpireAsync(key, KeyTtl);
            }
        }
    }

    private static string GetKey(string occurrenceId) => $"room:{occurrenceId}:participants";
}

/// <summary>
/// JSON-serializable entry stored in Redis for each participant.
/// </summary>
internal sealed record ParticipantCacheEntry(
    string? UserId,
    string DisplayName,
    string Role,
    string AudioState,
    string VideoState,
    DateTimeOffset JoinedAt);

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
