using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.Rooms.Domain.Occurrence;
using Muntada.Rooms.Domain.Template;
using Muntada.Rooms.Infrastructure;
using Muntada.SharedKernel.Domain.Exceptions;

namespace Muntada.Rooms.Application.Commands;

/// <summary>
/// Command to update a room occurrence (single-occurrence override for title, settings, or cancellation).
/// </summary>
/// <param name="TenantId">The owning tenant's identifier.</param>
/// <param name="OccurrenceId">The occurrence to update.</param>
/// <param name="Title">Updated title (null to keep current).</param>
/// <param name="MaxParticipants">Updated max participants (null to keep current settings).</param>
/// <param name="AllowGuestAccess">Updated guest access setting.</param>
/// <param name="AllowRecording">Updated recording setting.</param>
/// <param name="AllowTranscription">Updated transcription setting.</param>
/// <param name="DefaultTranscriptionLanguage">Updated transcription language.</param>
/// <param name="AutoStartRecording">Updated auto-start recording setting.</param>
/// <param name="UpdateSettings">Whether settings should be updated.</param>
/// <param name="IsCancelled">Updated cancellation flag (null to keep current).</param>
public sealed record UpdateRoomOccurrenceCommand(
    string TenantId,
    string OccurrenceId,
    string? Title,
    int? MaxParticipants,
    bool? AllowGuestAccess,
    bool? AllowRecording,
    bool? AllowTranscription,
    string? DefaultTranscriptionLanguage,
    bool? AutoStartRecording,
    bool UpdateSettings,
    bool? IsCancelled) : IRequest<RoomOccurrence>;

/// <summary>
/// Handles <see cref="UpdateRoomOccurrenceCommand"/> — applies single-occurrence overrides
/// (title, settings, cancel) to an existing room occurrence.
/// </summary>
public sealed class UpdateRoomOccurrenceCommandHandler : IRequestHandler<UpdateRoomOccurrenceCommand, RoomOccurrence>
{
    private readonly RoomsDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateRoomOccurrenceCommandHandler"/> class.
    /// </summary>
    public UpdateRoomOccurrenceCommandHandler(RoomsDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<RoomOccurrence> Handle(UpdateRoomOccurrenceCommand request, CancellationToken cancellationToken)
    {
        var occurrenceId = new RoomOccurrenceId(request.OccurrenceId);
        var occurrence = await _db.RoomOccurrences
            .FirstOrDefaultAsync(o => o.Id == occurrenceId && o.TenantId == request.TenantId, cancellationToken);

        if (occurrence is null)
            throw new EntityNotFoundException(nameof(RoomOccurrence), request.OccurrenceId);

        RoomSettings? newSettings = null;
        if (request.UpdateSettings)
        {
            newSettings = RoomSettings.Create(
                request.MaxParticipants ?? occurrence.Settings.MaxParticipants,
                request.AllowGuestAccess ?? occurrence.Settings.AllowGuestAccess,
                request.AllowRecording ?? occurrence.Settings.AllowRecording,
                request.AllowTranscription ?? occurrence.Settings.AllowTranscription,
                request.DefaultTranscriptionLanguage ?? occurrence.Settings.DefaultTranscriptionLanguage,
                request.AutoStartRecording ?? occurrence.Settings.AutoStartRecording);
        }

        occurrence.UpdateOverride(request.Title, newSettings, request.IsCancelled);
        occurrence.IncrementVersion();

        await _db.SaveChangesAsync(cancellationToken);

        return occurrence;
    }
}
