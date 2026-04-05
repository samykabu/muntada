using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Muntada.Rooms.Domain.Occurrence;
using Muntada.Rooms.Domain.Participant;
using Muntada.Rooms.Infrastructure.Cache;

namespace Muntada.Rooms.Infrastructure.Services;

/// <summary>
/// Handles LiveKit webhook events for participant tracking.
/// Provides HMAC-SHA256 signature verification and idempotent event processing.
/// Updates both the Redis cache (real-time) and SQL (persistent record).
/// </summary>
public class LiveKitWebhookHandler
{
    private readonly RoomsDbContext _db;
    private readonly ParticipantStateCache _cache;
    private readonly ILogger<LiveKitWebhookHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LiveKitWebhookHandler"/> class.
    /// </summary>
    /// <param name="db">The rooms database context.</param>
    /// <param name="cache">The participant state cache.</param>
    /// <param name="logger">The logger instance.</param>
    public LiveKitWebhookHandler(
        RoomsDbContext db,
        ParticipantStateCache cache,
        ILogger<LiveKitWebhookHandler> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Verifies the HMAC-SHA256 signature of an incoming LiveKit webhook request.
    /// </summary>
    /// <param name="payload">The raw request body.</param>
    /// <param name="signature">The signature from the Authorization header.</param>
    /// <param name="apiSecret">The LiveKit API secret used for signing.</param>
    /// <returns>True if the signature is valid.</returns>
    public static bool VerifySignature(string payload, string signature, string apiSecret)
    {
        if (string.IsNullOrEmpty(apiSecret) || string.IsNullOrEmpty(signature))
            return false;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(apiSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var computedSignature = Convert.ToBase64String(hash);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedSignature),
            Encoding.UTF8.GetBytes(signature));
    }

    /// <summary>
    /// Handles a participant_joined event from LiveKit.
    /// Creates a participant state record and updates the Redis cache.
    /// </summary>
    /// <param name="occurrenceId">The room occurrence identifier.</param>
    /// <param name="liveKitParticipantId">The LiveKit participant identifier.</param>
    /// <param name="userId">The authenticated user ID, or null for guests.</param>
    /// <param name="displayName">The participant's display name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task HandleParticipantJoinedAsync(
        string occurrenceId,
        string liveKitParticipantId,
        string? userId,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        var roomOccurrenceId = new RoomOccurrenceId(occurrenceId);

        // Idempotent: check if participant already tracked
        var existing = await _db.RoomParticipantStates
            .FirstOrDefaultAsync(
                p => p.RoomOccurrenceId == roomOccurrenceId
                    && p.LiveKitParticipantId == liveKitParticipantId
                    && p.LeftAt == null,
                cancellationToken);

        if (existing is not null)
        {
            _logger.LogDebug(
                "Participant {LiveKitId} already tracked for occurrence {OccurrenceId}, skipping duplicate join event.",
                liveKitParticipantId, occurrenceId);
            return;
        }

        var participant = RoomParticipantState.Create(
            roomOccurrenceId,
            userId,
            displayName,
            ParticipantRole.Member,
            liveKitParticipantId);

        _db.RoomParticipantStates.Add(participant);
        await _db.SaveChangesAsync(cancellationToken);

        // Update Redis cache
        await _cache.AddParticipantAsync(
            occurrenceId,
            participant.Id.Value,
            userId,
            displayName,
            ParticipantRole.Member);

        _logger.LogInformation(
            "Participant {LiveKitId} joined occurrence {OccurrenceId}.",
            liveKitParticipantId, occurrenceId);
    }

    /// <summary>
    /// Handles a participant_left event from LiveKit.
    /// Records the leave time and removes from Redis cache.
    /// </summary>
    /// <param name="occurrenceId">The room occurrence identifier.</param>
    /// <param name="liveKitParticipantId">The LiveKit participant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task HandleParticipantLeftAsync(
        string occurrenceId,
        string liveKitParticipantId,
        CancellationToken cancellationToken = default)
    {
        var roomOccurrenceId = new RoomOccurrenceId(occurrenceId);
        var participant = await _db.RoomParticipantStates
            .FirstOrDefaultAsync(
                p => p.RoomOccurrenceId == roomOccurrenceId
                    && p.LiveKitParticipantId == liveKitParticipantId
                    && p.LeftAt == null,
                cancellationToken);

        if (participant is null)
        {
            _logger.LogWarning(
                "No active participant found for LiveKit ID {LiveKitId} in occurrence {OccurrenceId} for leave event.",
                liveKitParticipantId, occurrenceId);
            return;
        }

        participant.RecordLeave();
        await _db.SaveChangesAsync(cancellationToken);

        // Remove from Redis cache
        await _cache.RemoveParticipantAsync(occurrenceId, participant.Id.Value);

        _logger.LogInformation(
            "Participant {LiveKitId} left occurrence {OccurrenceId}.",
            liveKitParticipantId, occurrenceId);
    }

    /// <summary>
    /// Handles track_published and track_unpublished events from LiveKit.
    /// Updates the participant's audio/video state in both SQL and Redis.
    /// </summary>
    /// <param name="occurrenceId">The room occurrence identifier.</param>
    /// <param name="liveKitParticipantId">The LiveKit participant identifier.</param>
    /// <param name="trackKind">The track kind (audio or video).</param>
    /// <param name="isPublished">True if published, false if unpublished.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task HandleTrackChangedAsync(
        string occurrenceId,
        string liveKitParticipantId,
        string trackKind,
        bool isPublished,
        CancellationToken cancellationToken = default)
    {
        var roomOccurrenceId = new RoomOccurrenceId(occurrenceId);
        var participant = await _db.RoomParticipantStates
            .FirstOrDefaultAsync(
                p => p.RoomOccurrenceId == roomOccurrenceId
                    && p.LiveKitParticipantId == liveKitParticipantId
                    && p.LeftAt == null,
                cancellationToken);

        if (participant is null)
        {
            _logger.LogWarning(
                "No active participant found for LiveKit ID {LiveKitId} in occurrence {OccurrenceId} for track change.",
                liveKitParticipantId, occurrenceId);
            return;
        }

        if (trackKind.Equals("audio", StringComparison.OrdinalIgnoreCase))
        {
            participant.UpdateAudioState(isPublished ? MediaState.Unmuted : MediaState.Muted);
        }
        else if (trackKind.Equals("video", StringComparison.OrdinalIgnoreCase))
        {
            participant.UpdateVideoState(isPublished ? MediaState.On : MediaState.Off);
        }

        await _db.SaveChangesAsync(cancellationToken);

        // Update Redis cache
        await _cache.UpdateMediaStateAsync(
            occurrenceId,
            participant.Id.Value,
            participant.AudioState,
            participant.VideoState);

        _logger.LogDebug(
            "Track {TrackKind} {Action} for participant {LiveKitId} in occurrence {OccurrenceId}.",
            trackKind, isPublished ? "published" : "unpublished", liveKitParticipantId, occurrenceId);
    }
}
