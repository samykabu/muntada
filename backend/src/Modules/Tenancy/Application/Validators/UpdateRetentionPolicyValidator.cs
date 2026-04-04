using FluentValidation;
using Muntada.Tenancy.Application.Commands;
using Muntada.Tenancy.Domain.Retention;

namespace Muntada.Tenancy.Application.Validators;

/// <summary>
/// Validates the <see cref="UpdateRetentionPolicyCommand"/> ensuring all provided retention
/// values fall within the allowed range (1-3650 days) and that audit log retention meets
/// the minimum compliance threshold of 2555 days (approximately 7 years).
/// </summary>
public sealed class UpdateRetentionPolicyValidator : AbstractValidator<UpdateRetentionPolicyCommand>
{
    /// <summary>
    /// Initializes a new instance of <see cref="UpdateRetentionPolicyValidator"/>
    /// with all retention policy validation rules.
    /// </summary>
    public UpdateRetentionPolicyValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEqual(Guid.Empty).WithMessage("TenantId must not be an empty identifier.");

        RuleFor(x => x.RecordingDays)
            .InclusiveBetween(RetentionPolicy.MinRetentionDays, RetentionPolicy.MaxRetentionDays)
            .WithMessage($"Recording retention must be between {RetentionPolicy.MinRetentionDays} and {RetentionPolicy.MaxRetentionDays} days.")
            .When(x => x.RecordingDays.HasValue);

        RuleFor(x => x.ChatDays)
            .InclusiveBetween(RetentionPolicy.MinRetentionDays, RetentionPolicy.MaxRetentionDays)
            .WithMessage($"Chat retention must be between {RetentionPolicy.MinRetentionDays} and {RetentionPolicy.MaxRetentionDays} days.")
            .When(x => x.ChatDays.HasValue);

        RuleFor(x => x.FileDays)
            .InclusiveBetween(RetentionPolicy.MinRetentionDays, RetentionPolicy.MaxRetentionDays)
            .WithMessage($"File retention must be between {RetentionPolicy.MinRetentionDays} and {RetentionPolicy.MaxRetentionDays} days.")
            .When(x => x.FileDays.HasValue);

        RuleFor(x => x.AuditLogDays)
            .InclusiveBetween(RetentionPolicy.MinAuditLogRetentionDays, RetentionPolicy.MaxRetentionDays)
            .WithMessage($"Audit log retention must be between {RetentionPolicy.MinAuditLogRetentionDays} and {RetentionPolicy.MaxRetentionDays} days.")
            .When(x => x.AuditLogDays.HasValue);

        RuleFor(x => x.ActivityDays)
            .InclusiveBetween(RetentionPolicy.MinRetentionDays, RetentionPolicy.MaxRetentionDays)
            .WithMessage($"Activity log retention must be between {RetentionPolicy.MinRetentionDays} and {RetentionPolicy.MaxRetentionDays} days.")
            .When(x => x.ActivityDays.HasValue);
    }
}
