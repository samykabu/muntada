using Muntada.SharedKernel.Domain;

namespace Muntada.Rooms.Domain.Events;

// ────────────────────────────────────────────────────────────────
// Domain Events (in-process, handled by MediatR)
// ────────────────────────────────────────────────────────────────

/// <summary>
/// Domain event raised when a room occurrence transitions to a new status.
/// </summary>
public sealed record RoomStatusChangedDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string OccurrenceId,
    string FromStatus,
    string ToStatus) : IDomainEvent;

/// <summary>
/// Domain event raised when a participant joins or leaves a room.
/// </summary>
public sealed record ParticipantChangedDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string OccurrenceId,
    string ParticipantStateId,
    string ChangeType) : IDomainEvent;

// ────────────────────────────────────────────────────────────────
// Integration Events (cross-module, published via MassTransit)
// ────────────────────────────────────────────────────────────────

/// <summary>
/// Published when a new room template is created.
/// </summary>
public sealed record RoomTemplateCreatedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string AggregateId,
    string AggregateType,
    int Version,
    string TemplateId,
    string TenantId,
    string Name) : IIntegrationEvent;

/// <summary>
/// Published when a new recurring room series is created.
/// </summary>
public sealed record RoomSeriesCreatedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string AggregateId,
    string AggregateType,
    int Version,
    string SeriesId,
    string TenantId,
    string RecurrenceRule) : IIntegrationEvent;

/// <summary>
/// Published when a room occurrence is generated from a series or created standalone.
/// </summary>
public sealed record RoomOccurrenceGeneratedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string AggregateId,
    string AggregateType,
    int Version,
    string OccurrenceId,
    string TenantId,
    DateTimeOffset ScheduledAt) : IIntegrationEvent;

/// <summary>
/// Published when a room occurrence is scheduled (moderator and time set).
/// </summary>
public sealed record RoomScheduledEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string AggregateId,
    string AggregateType,
    int Version,
    string OccurrenceId,
    DateTimeOffset ScheduledAt) : IIntegrationEvent;

/// <summary>
/// Published when a room transitions to Live status (first participant connects).
/// </summary>
public sealed record RoomLiveEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string AggregateId,
    string AggregateType,
    int Version,
    string OccurrenceId,
    DateTimeOffset LiveStartedAt) : IIntegrationEvent;

/// <summary>
/// Published when a room enters Grace period (moderator disconnected).
/// </summary>
public sealed record RoomGraceStartedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string AggregateId,
    string AggregateType,
    int Version,
    string OccurrenceId,
    DateTimeOffset GraceStartedAt,
    int GracePeriodSeconds) : IIntegrationEvent;

/// <summary>
/// Published when a room transitions to Ended status.
/// </summary>
public sealed record RoomEndedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string AggregateId,
    string AggregateType,
    int Version,
    string OccurrenceId,
    DateTimeOffset LiveEndedAt) : IIntegrationEvent;

/// <summary>
/// Published when a room is archived per retention policy.
/// </summary>
public sealed record RoomArchivedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string AggregateId,
    string AggregateType,
    int Version,
    string OccurrenceId) : IIntegrationEvent;

/// <summary>
/// Published when a participant joins a room.
/// </summary>
public sealed record ParticipantJoinedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string AggregateId,
    string AggregateType,
    int Version,
    string OccurrenceId,
    string? UserId,
    string DisplayName,
    string Role) : IIntegrationEvent;

/// <summary>
/// Published when a participant leaves a room.
/// </summary>
public sealed record ParticipantLeftEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string AggregateId,
    string AggregateType,
    int Version,
    string OccurrenceId,
    string? UserId,
    DateTimeOffset LeftAt) : IIntegrationEvent;

/// <summary>
/// Published when a moderator is assigned to a room.
/// </summary>
public sealed record ModeratorAssignedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string AggregateId,
    string AggregateType,
    int Version,
    string OccurrenceId,
    string UserId) : IIntegrationEvent;

/// <summary>
/// Published when moderator control is handed over to another user.
/// </summary>
public sealed record ModeratorHandoverEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string AggregateId,
    string AggregateType,
    int Version,
    string OccurrenceId,
    string FromUserId,
    string ToUserId) : IIntegrationEvent;

/// <summary>
/// Published when a room invite is sent.
/// </summary>
public sealed record RoomInviteSentEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string AggregateId,
    string AggregateType,
    int Version,
    string InviteId,
    string OccurrenceId,
    string? InvitedEmail) : IIntegrationEvent;

/// <summary>
/// Published when recording starts in a room.
/// </summary>
public sealed record RecordingStartedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string AggregateId,
    string AggregateType,
    int Version,
    string RecordingId,
    string OccurrenceId) : IIntegrationEvent;

/// <summary>
/// Published when recording completes and is ready for access.
/// </summary>
public sealed record RecordingCompletedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string AggregateId,
    string AggregateType,
    int Version,
    string RecordingId,
    string S3Path,
    long DurationSeconds) : IIntegrationEvent;

/// <summary>
/// Published when transcription completes for a recording.
/// </summary>
public sealed record TranscriptionCompletedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string AggregateId,
    string AggregateType,
    int Version,
    string RecordingId,
    string Language,
    string S3Path) : IIntegrationEvent;
