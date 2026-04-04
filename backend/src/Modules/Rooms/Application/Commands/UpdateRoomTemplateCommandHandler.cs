using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.Rooms.Domain.Template;
using Muntada.Rooms.Infrastructure;
using Muntada.SharedKernel.Domain.Exceptions;

namespace Muntada.Rooms.Application.Commands;

/// <summary>
/// Command to update an existing room template. Name is immutable.
/// </summary>
/// <param name="TenantId">The owning tenant's identifier.</param>
/// <param name="TemplateId">The template to update.</param>
/// <param name="Description">Updated description (null to clear).</param>
/// <param name="MaxParticipants">Updated max participants.</param>
/// <param name="AllowGuestAccess">Updated guest access setting.</param>
/// <param name="AllowRecording">Updated recording setting.</param>
/// <param name="AllowTranscription">Updated transcription setting.</param>
/// <param name="DefaultTranscriptionLanguage">Updated transcription language.</param>
/// <param name="AutoStartRecording">Updated auto-start recording setting.</param>
public sealed record UpdateRoomTemplateCommand(
    string TenantId,
    string TemplateId,
    string? Description,
    int MaxParticipants,
    bool AllowGuestAccess,
    bool AllowRecording,
    bool AllowTranscription,
    string? DefaultTranscriptionLanguage,
    bool AutoStartRecording) : IRequest<RoomTemplate>;

/// <summary>
/// Handles <see cref="UpdateRoomTemplateCommand"/> — updates all fields except name.
/// </summary>
public sealed class UpdateRoomTemplateCommandHandler : IRequestHandler<UpdateRoomTemplateCommand, RoomTemplate>
{
    private readonly RoomsDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateRoomTemplateCommandHandler"/> class.
    /// </summary>
    public UpdateRoomTemplateCommandHandler(RoomsDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<RoomTemplate> Handle(UpdateRoomTemplateCommand request, CancellationToken cancellationToken)
    {
        var templateId = new RoomTemplateId(request.TemplateId);
        var template = await _db.RoomTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId && t.TenantId == request.TenantId, cancellationToken);

        if (template is null)
            throw new EntityNotFoundException(nameof(RoomTemplate), request.TemplateId);

        var newSettings = RoomSettings.Create(
            request.MaxParticipants,
            request.AllowGuestAccess,
            request.AllowRecording,
            request.AllowTranscription,
            request.DefaultTranscriptionLanguage,
            request.AutoStartRecording);

        template.Update(request.Description, newSettings);
        template.IncrementVersion();

        await _db.SaveChangesAsync(cancellationToken);

        return template;
    }
}
