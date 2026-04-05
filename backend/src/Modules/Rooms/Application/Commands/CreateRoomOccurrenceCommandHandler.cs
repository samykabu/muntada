using MediatR;
using Muntada.Rooms.Domain.Occurrence;
using Muntada.Rooms.Domain.Template;
using Muntada.Rooms.Infrastructure;

namespace Muntada.Rooms.Application.Commands;

/// <summary>
/// Command to create a standalone room occurrence (not linked to a series).
/// </summary>
/// <param name="TenantId">The owning tenant's identifier.</param>
/// <param name="Title">The room title (3-200 chars).</param>
/// <param name="ScheduledAt">The UTC scheduled start time.</param>
/// <param name="OrganizerTimeZoneId">IANA timezone identifier for display conversion.</param>
/// <param name="ModeratorUserId">The user ID of the designated moderator.</param>
/// <param name="MaxParticipants">Maximum participants allowed.</param>
/// <param name="AllowGuestAccess">Whether guests can join via magic link.</param>
/// <param name="AllowRecording">Whether recording is enabled.</param>
/// <param name="AllowTranscription">Whether transcription is enabled.</param>
/// <param name="DefaultTranscriptionLanguage">ISO 639-1 language code for transcription.</param>
/// <param name="AutoStartRecording">Whether recording starts automatically.</param>
/// <param name="GracePeriodSeconds">Grace period timeout in seconds (default 300).</param>
/// <param name="CreatedBy">The user creating the occurrence.</param>
public sealed record CreateRoomOccurrenceCommand(
    string TenantId,
    string Title,
    DateTimeOffset ScheduledAt,
    string OrganizerTimeZoneId,
    string ModeratorUserId,
    int MaxParticipants,
    bool AllowGuestAccess,
    bool AllowRecording,
    bool AllowTranscription,
    string? DefaultTranscriptionLanguage,
    bool AutoStartRecording,
    int GracePeriodSeconds,
    string CreatedBy) : IRequest<RoomOccurrence>;

/// <summary>
/// Handles <see cref="CreateRoomOccurrenceCommand"/> — creates a standalone room occurrence
/// with moderator assignment and schedules it.
/// </summary>
public sealed class CreateRoomOccurrenceCommandHandler : IRequestHandler<CreateRoomOccurrenceCommand, RoomOccurrence>
{
    private readonly RoomsDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateRoomOccurrenceCommandHandler"/> class.
    /// </summary>
    public CreateRoomOccurrenceCommandHandler(RoomsDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<RoomOccurrence> Handle(CreateRoomOccurrenceCommand request, CancellationToken cancellationToken)
    {
        var settings = RoomSettings.Create(
            request.MaxParticipants,
            request.AllowGuestAccess,
            request.AllowRecording,
            request.AllowTranscription,
            request.DefaultTranscriptionLanguage,
            request.AutoStartRecording);

        var occurrence = RoomOccurrence.CreateStandalone(
            request.TenantId,
            request.Title,
            request.ScheduledAt,
            request.OrganizerTimeZoneId,
            settings,
            request.CreatedBy,
            request.GracePeriodSeconds);

        occurrence.AssignModeratorAndSchedule(request.ModeratorUserId);

        _db.RoomOccurrences.Add(occurrence);
        await _db.SaveChangesAsync(cancellationToken);

        return occurrence;
    }
}
