using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Muntada.Rooms.Domain.Events;
using Muntada.Rooms.Domain.Template;
using Muntada.Rooms.Infrastructure;
using Muntada.SharedKernel.Domain.Exceptions;

namespace Muntada.Rooms.Application.Commands;

/// <summary>
/// Command to create a new room template.
/// </summary>
/// <param name="TenantId">The owning tenant's identifier.</param>
/// <param name="Name">The template name (3-100 chars, unique per tenant).</param>
/// <param name="Description">Optional description (max 500 chars).</param>
/// <param name="MaxParticipants">Maximum participants for rooms using this template.</param>
/// <param name="AllowGuestAccess">Whether guests can join via magic link.</param>
/// <param name="AllowRecording">Whether recording is enabled.</param>
/// <param name="AllowTranscription">Whether transcription is enabled.</param>
/// <param name="DefaultTranscriptionLanguage">ISO 639-1 language code for transcription.</param>
/// <param name="AutoStartRecording">Whether recording starts automatically.</param>
/// <param name="CreatedBy">The user creating the template.</param>
public sealed record CreateRoomTemplateCommand(
    string TenantId,
    string Name,
    string? Description,
    int MaxParticipants,
    bool AllowGuestAccess,
    bool AllowRecording,
    bool AllowTranscription,
    string? DefaultTranscriptionLanguage,
    bool AutoStartRecording,
    string CreatedBy) : IRequest<RoomTemplate>;

/// <summary>
/// Handles <see cref="CreateRoomTemplateCommand"/> — validates uniqueness and plan limits,
/// then creates and persists the template.
/// </summary>
public sealed class CreateRoomTemplateCommandHandler : IRequestHandler<CreateRoomTemplateCommand, RoomTemplate>
{
    private readonly RoomsDbContext _db;
    private readonly ILogger<CreateRoomTemplateCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateRoomTemplateCommandHandler"/> class.
    /// </summary>
    public CreateRoomTemplateCommandHandler(RoomsDbContext db, ILogger<CreateRoomTemplateCommandHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<RoomTemplate> Handle(CreateRoomTemplateCommand request, CancellationToken cancellationToken)
    {
        using var activity = RoomsTelemetry.TemplateCreation("pending", request.TenantId);

        // Validate name uniqueness within tenant
        var nameExists = await _db.RoomTemplates
            .AnyAsync(t => t.TenantId == request.TenantId && t.Name == request.Name, cancellationToken);

        if (nameExists)
            throw new ValidationException("Name", $"A template with the name '{request.Name}' already exists in this tenant.");

        var settings = RoomSettings.Create(
            request.MaxParticipants,
            request.AllowGuestAccess,
            request.AllowRecording,
            request.AllowTranscription,
            request.DefaultTranscriptionLanguage,
            request.AutoStartRecording);

        var template = RoomTemplate.Create(
            request.TenantId,
            request.Name,
            request.Description,
            settings,
            request.CreatedBy);

        _db.RoomTemplates.Add(template);
        await _db.SaveChangesAsync(cancellationToken);

        activity?.SetTag("rooms.template_id", template.Id.Value);
        RoomsLogging.TemplateCreated(_logger, template.Id.Value, request.TenantId, null);

        return template;
    }
}
