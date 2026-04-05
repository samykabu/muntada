using System.Diagnostics;

namespace Muntada.Rooms.Infrastructure;

/// <summary>
/// Provides OpenTelemetry instrumentation for the Rooms module.
/// Defines an <see cref="ActivitySource"/> named <c>Muntada.Rooms</c> and
/// static factory methods for creating activities (spans) around key operations.
/// </summary>
public static class RoomsTelemetry
{
    /// <summary>
    /// The name of the <see cref="ActivitySource"/> for the Rooms module.
    /// </summary>
    public const string SourceName = "Muntada.Rooms";

    /// <summary>
    /// The shared <see cref="ActivitySource"/> for creating Rooms module spans.
    /// </summary>
    public static readonly ActivitySource ActivitySource = new(SourceName);

    /// <summary>
    /// Creates an activity for room template creation.
    /// </summary>
    public static Activity? TemplateCreation(string templateId, string tenantId)
    {
        var activity = ActivitySource.StartActivity("Rooms.TemplateCreation");
        activity?.SetTag("rooms.template_id", templateId);
        activity?.SetTag("rooms.tenant_id", tenantId);
        activity?.SetTag("rooms.operation", "template_create");
        return activity;
    }

    /// <summary>
    /// Creates an activity for room series creation.
    /// </summary>
    public static Activity? SeriesCreation(string seriesId, string tenantId, string recurrenceRule)
    {
        var activity = ActivitySource.StartActivity("Rooms.SeriesCreation");
        activity?.SetTag("rooms.series_id", seriesId);
        activity?.SetTag("rooms.tenant_id", tenantId);
        activity?.SetTag("rooms.recurrence_rule", recurrenceRule);
        activity?.SetTag("rooms.operation", "series_create");
        return activity;
    }

    /// <summary>
    /// Creates an activity for room status transitions.
    /// </summary>
    public static Activity? RoomTransition(string occurrenceId, string fromStatus, string toStatus)
    {
        var activity = ActivitySource.StartActivity("Rooms.RoomTransition");
        activity?.SetTag("rooms.occurrence_id", occurrenceId);
        activity?.SetTag("rooms.status.from", fromStatus);
        activity?.SetTag("rooms.status.to", toStatus);
        activity?.SetTag("rooms.operation", "room_transition");
        return activity;
    }

    /// <summary>
    /// Creates an activity for participant join operations.
    /// </summary>
    public static Activity? ParticipantJoin(string occurrenceId, string? userId, string role)
    {
        var activity = ActivitySource.StartActivity("Rooms.ParticipantJoin");
        activity?.SetTag("rooms.occurrence_id", occurrenceId);
        activity?.SetTag("rooms.user_id", userId ?? "guest");
        activity?.SetTag("rooms.participant_role", role);
        activity?.SetTag("rooms.operation", "participant_join");
        return activity;
    }

    /// <summary>
    /// Creates an activity for invite generation.
    /// </summary>
    public static Activity? InviteGeneration(string occurrenceId, string inviteType, int count)
    {
        var activity = ActivitySource.StartActivity("Rooms.InviteGeneration");
        activity?.SetTag("rooms.occurrence_id", occurrenceId);
        activity?.SetTag("rooms.invite_type", inviteType);
        activity?.SetTag("rooms.invite_count", count);
        activity?.SetTag("rooms.operation", "invite_generate");
        return activity;
    }

    /// <summary>
    /// Creates an activity for recording operations.
    /// </summary>
    public static Activity? RecordingOperation(string occurrenceId, string recordingId, string operation)
    {
        var activity = ActivitySource.StartActivity("Rooms.RecordingOperation");
        activity?.SetTag("rooms.occurrence_id", occurrenceId);
        activity?.SetTag("rooms.recording_id", recordingId);
        activity?.SetTag("rooms.operation", operation);
        return activity;
    }

    /// <summary>
    /// Creates an activity for LiveKit webhook processing.
    /// </summary>
    public static Activity? WebhookProcessing(string eventType, string? occurrenceId)
    {
        var activity = ActivitySource.StartActivity("Rooms.WebhookProcessing");
        activity?.SetTag("rooms.webhook_event_type", eventType);
        activity?.SetTag("rooms.occurrence_id", occurrenceId ?? "unknown");
        activity?.SetTag("rooms.operation", "webhook_process");
        return activity;
    }
}
