using Microsoft.Extensions.Logging;

namespace Muntada.Rooms.Infrastructure;

/// <summary>
/// Provides high-performance structured logging definitions for the Rooms module
/// using <c>LoggerMessage.Define</c>. Each definition uses a compile-time generated
/// delegate to avoid boxing and string formatting overhead at runtime.
/// </summary>
/// <remarks>
/// Event ID ranges for Rooms module: 3000-3099.
/// </remarks>
public static class RoomsLogging
{
    // ──────────────────────────────────────────────────────
    // Room templates (3000-3009)
    // ──────────────────────────────────────────────────────

    /// <summary>Logs that a room template was created.</summary>
    public static readonly Action<ILogger, string, string, Exception?> TemplateCreated =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(3000, nameof(TemplateCreated)),
            "Room template created: TemplateId={TemplateId}, TenantId={TenantId}");

    /// <summary>Logs that a room template was updated.</summary>
    public static readonly Action<ILogger, string, Exception?> TemplateUpdated =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(3001, nameof(TemplateUpdated)),
            "Room template updated: TemplateId={TemplateId}");

    // ──────────────────────────────────────────────────────
    // Room series (3010-3019)
    // ──────────────────────────────────────────────────────

    /// <summary>Logs that a room series was created.</summary>
    public static readonly Action<ILogger, string, string, string, Exception?> SeriesCreated =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Information,
            new EventId(3010, nameof(SeriesCreated)),
            "Room series created: SeriesId={SeriesId}, TenantId={TenantId}, RRULE={RecurrenceRule}");

    /// <summary>Logs that occurrences were generated for a series.</summary>
    public static readonly Action<ILogger, string, int, Exception?> OccurrencesGenerated =
        LoggerMessage.Define<string, int>(
            LogLevel.Information,
            new EventId(3011, nameof(OccurrencesGenerated)),
            "Occurrences generated: SeriesId={SeriesId}, Count={Count}");

    // ──────────────────────────────────────────────────────
    // Room lifecycle (3020-3039)
    // ──────────────────────────────────────────────────────

    /// <summary>Logs a room status transition.</summary>
    public static readonly Action<ILogger, string, string, string, Exception?> RoomTransition =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Information,
            new EventId(3020, nameof(RoomTransition)),
            "Room transition: OccurrenceId={OccurrenceId}, From={FromStatus}, To={ToStatus}");

    /// <summary>Logs that a room grace period started.</summary>
    public static readonly Action<ILogger, string, int, Exception?> GracePeriodStarted =
        LoggerMessage.Define<string, int>(
            LogLevel.Warning,
            new EventId(3021, nameof(GracePeriodStarted)),
            "Grace period started: OccurrenceId={OccurrenceId}, TimeoutSeconds={TimeoutSeconds}");

    /// <summary>Logs that a grace period was cancelled (moderator reconnected).</summary>
    public static readonly Action<ILogger, string, Exception?> GracePeriodCancelled =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(3022, nameof(GracePeriodCancelled)),
            "Grace period cancelled: OccurrenceId={OccurrenceId}");

    /// <summary>Logs that a grace period expired and room ended.</summary>
    public static readonly Action<ILogger, string, Exception?> GracePeriodExpired =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(3023, nameof(GracePeriodExpired)),
            "Grace period expired, room ending: OccurrenceId={OccurrenceId}");

    /// <summary>Logs an invalid state transition attempt.</summary>
    public static readonly Action<ILogger, string, string, string, Exception?> InvalidTransition =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Warning,
            new EventId(3024, nameof(InvalidTransition)),
            "Invalid room transition rejected: OccurrenceId={OccurrenceId}, CurrentStatus={CurrentStatus}, AttemptedStatus={AttemptedStatus}");

    // ──────────────────────────────────────────────────────
    // Participants (3040-3049)
    // ──────────────────────────────────────────────────────

    /// <summary>Logs that a participant joined a room.</summary>
    public static readonly Action<ILogger, string, string, string, Exception?> ParticipantJoined =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Information,
            new EventId(3040, nameof(ParticipantJoined)),
            "Participant joined: OccurrenceId={OccurrenceId}, DisplayName={DisplayName}, Role={Role}");

    /// <summary>Logs that a participant left a room.</summary>
    public static readonly Action<ILogger, string, string, Exception?> ParticipantLeft =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(3041, nameof(ParticipantLeft)),
            "Participant left: OccurrenceId={OccurrenceId}, ParticipantId={ParticipantId}");

    // ──────────────────────────────────────────────────────
    // Invites (3050-3059)
    // ──────────────────────────────────────────────────────

    /// <summary>Logs that a room invite was sent.</summary>
    public static readonly Action<ILogger, string, string, string, Exception?> InviteSent =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Information,
            new EventId(3050, nameof(InviteSent)),
            "Invite sent: OccurrenceId={OccurrenceId}, InviteId={InviteId}, Type={InviteType}");

    /// <summary>Logs that a room invite was revoked.</summary>
    public static readonly Action<ILogger, string, Exception?> InviteRevoked =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(3051, nameof(InviteRevoked)),
            "Invite revoked: InviteId={InviteId}");

    // ──────────────────────────────────────────────────────
    // Moderator (3060-3069)
    // ──────────────────────────────────────────────────────

    /// <summary>Logs that a moderator was assigned.</summary>
    public static readonly Action<ILogger, string, string, Exception?> ModeratorAssigned =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(3060, nameof(ModeratorAssigned)),
            "Moderator assigned: OccurrenceId={OccurrenceId}, UserId={UserId}");

    /// <summary>Logs a moderator handover.</summary>
    public static readonly Action<ILogger, string, string, string, Exception?> ModeratorHandover =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Information,
            new EventId(3061, nameof(ModeratorHandover)),
            "Moderator handover: OccurrenceId={OccurrenceId}, From={FromUserId}, To={ToUserId}");

    // ──────────────────────────────────────────────────────
    // Recording (3070-3079)
    // ──────────────────────────────────────────────────────

    /// <summary>Logs that recording started.</summary>
    public static readonly Action<ILogger, string, string, Exception?> RecordingStarted =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(3070, nameof(RecordingStarted)),
            "Recording started: OccurrenceId={OccurrenceId}, RecordingId={RecordingId}");

    /// <summary>Logs that recording completed.</summary>
    public static readonly Action<ILogger, string, string, long, Exception?> RecordingCompleted =
        LoggerMessage.Define<string, string, long>(
            LogLevel.Information,
            new EventId(3071, nameof(RecordingCompleted)),
            "Recording completed: RecordingId={RecordingId}, S3Path={S3Path}, DurationSeconds={DurationSeconds}");

    /// <summary>Logs that transcription completed.</summary>
    public static readonly Action<ILogger, string, string, Exception?> TranscriptionCompleted =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(3072, nameof(TranscriptionCompleted)),
            "Transcription completed: RecordingId={RecordingId}, Language={Language}");
}
