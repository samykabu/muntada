using FluentValidation;
using Muntada.Identity.Application.Commands;

namespace Muntada.Identity.Application.Validators;

/// <summary>
/// Validates the <see cref="RegisterUserCommand"/> ensuring email format,
/// password complexity, and password confirmation match.
/// </summary>
public sealed class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    /// <summary>
    /// Initializes a new instance of <see cref="RegisterUserValidator"/>
    /// with all registration validation rules.
    /// </summary>
    public RegisterUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(12).WithMessage("Password must be at least 12 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Password confirmation does not match.");
    }
}
