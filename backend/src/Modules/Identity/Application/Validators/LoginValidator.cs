using FluentValidation;
using Muntada.Identity.Application.Commands;

namespace Muntada.Identity.Application.Validators;

/// <summary>
/// Validates the <see cref="LoginCommand"/> ensuring required fields are provided.
/// </summary>
public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    /// <summary>
    /// Initializes a new instance of <see cref="LoginValidator"/>
    /// with all login validation rules.
    /// </summary>
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}
