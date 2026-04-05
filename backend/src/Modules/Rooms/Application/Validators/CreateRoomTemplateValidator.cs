using FluentValidation;
using Muntada.Rooms.Application.Commands;

namespace Muntada.Rooms.Application.Validators;

/// <summary>
/// FluentValidation validator for <see cref="CreateRoomTemplateCommand"/>.
/// </summary>
public sealed class CreateRoomTemplateValidator : AbstractValidator<CreateRoomTemplateCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateRoomTemplateValidator"/> class.
    /// </summary>
    public CreateRoomTemplateValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().Length(3, 100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.MaxParticipants).InclusiveBetween(1, 10_000);
        RuleFor(x => x.CreatedBy).NotEmpty();

        RuleFor(x => x.AllowTranscription)
            .Must((cmd, allowTranscription) => !allowTranscription || cmd.AllowRecording)
            .WithMessage("Transcription requires recording to be enabled.");

        RuleFor(x => x.DefaultTranscriptionLanguage)
            .MaximumLength(10)
            .When(x => x.AllowTranscription);
    }
}
