using FluentValidation;
using Muntada.Tenancy.Application.Commands;

namespace Muntada.Tenancy.Application.Validators;

/// <summary>
/// Validates the <see cref="CreateTenantCommand"/> ensuring tenant name length,
/// optional slug format, and a valid creator identifier.
/// </summary>
public sealed class CreateTenantValidator : AbstractValidator<CreateTenantCommand>
{
    /// <summary>
    /// Initializes a new instance of <see cref="CreateTenantValidator"/>
    /// with all tenant creation validation rules.
    /// </summary>
    public CreateTenantValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tenant name is required.")
            .MinimumLength(3).WithMessage("Tenant name must be at least 3 characters.")
            .MaximumLength(100).WithMessage("Tenant name must not exceed 100 characters.");

        RuleFor(x => x.Slug)
            .MinimumLength(3).WithMessage("Slug must be at least 3 characters.")
            .MaximumLength(63).WithMessage("Slug must not exceed 63 characters.")
            .Matches(@"^[a-z0-9][a-z0-9-]*[a-z0-9]$")
                .WithMessage("Slug must contain only lowercase alphanumeric characters and hyphens, " +
                             "and must start and end with an alphanumeric character.")
            .When(x => !string.IsNullOrWhiteSpace(x.Slug));

        RuleFor(x => x.CreatedBy)
            .NotEqual(Guid.Empty).WithMessage("CreatedBy must not be an empty identifier.");
    }
}
