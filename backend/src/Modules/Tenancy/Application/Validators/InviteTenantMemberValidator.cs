using FluentValidation;
using Muntada.Tenancy.Application.Commands;
using Muntada.Tenancy.Domain.Membership;

namespace Muntada.Tenancy.Application.Validators;

/// <summary>
/// Validates the <see cref="InviteTenantMemberCommand"/> ensuring email format,
/// valid role assignment, and a valid inviter identifier.
/// </summary>
public sealed class InviteTenantMemberValidator : AbstractValidator<InviteTenantMemberCommand>
{
    /// <summary>
    /// Initializes a new instance of <see cref="InviteTenantMemberValidator"/>
    /// with all invitation validation rules.
    /// </summary>
    public InviteTenantMemberValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEqual(Guid.Empty).WithMessage("TenantId must not be an empty identifier.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required.")
            .MaximumLength(256).WithMessage("Email address must not exceed 256 characters.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Role must be a valid TenantRole value (Owner, Admin, or Member).");

        RuleFor(x => x.InvitedBy)
            .NotEqual(Guid.Empty).WithMessage("InvitedBy must not be an empty identifier.");

        RuleFor(x => x.Message)
            .MaximumLength(500).WithMessage("Invitation message must not exceed 500 characters.")
            .When(x => x.Message is not null);
    }
}
