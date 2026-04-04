using FluentValidation;
using Muntada.Rooms.Application.Commands;

namespace Muntada.Rooms.Application.Validators;

/// <summary>
/// FluentValidation validator for <see cref="CreateRoomOccurrenceCommand"/>.
/// </summary>
public sealed class CreateRoomOccurrenceValidator : AbstractValidator<CreateRoomOccurrenceCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateRoomOccurrenceValidator"/> class.
    /// </summary>
    public CreateRoomOccurrenceValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().Length(3, 200);
        RuleFor(x => x.ScheduledAt).GreaterThan(DateTimeOffset.UtcNow)
            .WithMessage("Scheduled time must be in the future.");
        RuleFor(x => x.OrganizerTimeZoneId).NotEmpty();
        RuleFor(x => x.ModeratorUserId).NotEmpty();
        RuleFor(x => x.MaxParticipants).InclusiveBetween(1, 10_000);
        RuleFor(x => x.GracePeriodSeconds).InclusiveBetween(60, 1800);
        RuleFor(x => x.CreatedBy).NotEmpty();

        RuleFor(x => x.AllowTranscription)
            .Must((cmd, allowTranscription) => !allowTranscription || cmd.AllowRecording)
            .WithMessage("Transcription requires recording to be enabled.");

        RuleFor(x => x.DefaultTranscriptionLanguage)
            .MaximumLength(10)
            .When(x => x.AllowTranscription);
    }
}
