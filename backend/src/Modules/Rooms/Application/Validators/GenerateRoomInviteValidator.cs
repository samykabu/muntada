using FluentValidation;
using Muntada.Rooms.Application.Commands;
using Muntada.Rooms.Domain.Invite;

namespace Muntada.Rooms.Application.Validators;

/// <summary>
/// FluentValidation validator for <see cref="GenerateRoomInviteCommand"/>.
/// </summary>
public sealed class GenerateRoomInviteValidator : AbstractValidator<GenerateRoomInviteCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GenerateRoomInviteValidator"/> class.
    /// </summary>
    public GenerateRoomInviteValidator()
    {
        RuleFor(x => x.OccurrenceId).NotEmpty();
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.InvitedBy).NotEmpty();
        RuleFor(x => x.Invites).NotEmpty()
            .WithMessage("At least one invite is required.");
        RuleFor(x => x.Invites.Count).LessThanOrEqualTo(50)
            .WithMessage("Cannot send more than 50 invites in a single request.");

        RuleForEach(x => x.Invites).ChildRules(invite =>
        {
            invite.RuleFor(i => i.Email)
                .NotEmpty()
                .EmailAddress()
                .When(i => i.InviteType == RoomInviteType.Email)
                .WithMessage("A valid email address is required for email invites.");

            invite.RuleFor(i => i.InviteType)
                .IsInEnum()
                .WithMessage("Invalid invite type.");
        });
    }
}
