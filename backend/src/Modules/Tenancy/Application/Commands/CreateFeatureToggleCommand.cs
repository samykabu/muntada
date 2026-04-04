using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.SharedKernel.Application;
using Muntada.Tenancy.Domain.Events;
using Muntada.Tenancy.Domain.Features;
using Muntada.Tenancy.Infrastructure;

namespace Muntada.Tenancy.Application.Commands;

/// <summary>
/// Command to create a new feature toggle. The toggle is created disabled by default.
/// </summary>
/// <param name="FeatureName">The unique name identifying the feature toggle.</param>
/// <param name="Scope">The evaluation scope for the toggle (Global, PerTenant, Canary, etc.).</param>
public sealed record CreateFeatureToggleCommand(
    string FeatureName,
    FeatureToggleScope Scope) : IRequest<CreateFeatureToggleResult>;

/// <summary>
/// Result returned after successful feature toggle creation.
/// </summary>
/// <param name="Id">The unique identifier of the newly created toggle.</param>
/// <param name="FeatureName">The name of the feature toggle.</param>
/// <param name="IsEnabled">Whether the toggle is enabled (always <c>false</c> on creation).</param>
/// <param name="Scope">The evaluation scope of the toggle.</param>
/// <param name="CanaryPercentage">The canary rollout percentage (always 0 on creation).</param>
public sealed record CreateFeatureToggleResult(
    Guid Id,
    string FeatureName,
    bool IsEnabled,
    string Scope,
    int CanaryPercentage);

/// <summary>
/// Handles <see cref="CreateFeatureToggleCommand"/> by creating a new feature toggle
/// in the disabled state and publishing a <see cref="FeatureToggleChangedEvent"/>.
/// </summary>
public sealed class CreateFeatureToggleCommandHandler
    : IRequestHandler<CreateFeatureToggleCommand, CreateFeatureToggleResult>
{
    private readonly TenancyDbContext _dbContext;
    private readonly IIntegrationEventPublisher _eventPublisher;

    /// <summary>
    /// Initializes a new instance of <see cref="CreateFeatureToggleCommandHandler"/>.
    /// </summary>
    /// <param name="dbContext">The Tenancy module database context.</param>
    /// <param name="eventPublisher">Publisher for integration events.</param>
    public CreateFeatureToggleCommandHandler(
        TenancyDbContext dbContext,
        IIntegrationEventPublisher eventPublisher)
    {
        _dbContext = dbContext;
        _eventPublisher = eventPublisher;
    }

    /// <summary>
    /// Handles feature toggle creation: validates uniqueness, creates the toggle,
    /// persists it, and publishes the <see cref="FeatureToggleChangedEvent"/>.
    /// </summary>
    /// <param name="request">The feature toggle creation command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The creation result containing the new toggle's details.</returns>
    /// <exception cref="FluentValidation.ValidationException">
    /// Thrown when a toggle with the same name already exists.
    /// </exception>
    public async Task<CreateFeatureToggleResult> Handle(
        CreateFeatureToggleCommand request,
        CancellationToken cancellationToken)
    {
        var nameExists = await _dbContext.FeatureToggles
            .AnyAsync(f => f.FeatureName == request.FeatureName, cancellationToken);

        if (nameExists)
        {
            throw new FluentValidation.ValidationException(
                [new FluentValidation.Results.ValidationFailure(
                    "FeatureName",
                    $"A feature toggle with the name '{request.FeatureName}' already exists.")]);
        }

        var toggle = FeatureToggle.Create(request.FeatureName, request.Scope, isEnabled: false);

        _dbContext.FeatureToggles.Add(toggle);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Publish integration event
        var @event = new FeatureToggleChangedEvent(
            EventId: Guid.NewGuid(),
            OccurredAt: DateTimeOffset.UtcNow,
            AggregateId: toggle.Id.ToString(),
            AggregateType: nameof(FeatureToggle),
            Version: 1,
            FeatureName: toggle.FeatureName,
            IsEnabled: toggle.IsEnabled,
            Scope: toggle.Scope.ToString(),
            CanaryPercentage: toggle.CanaryPercentage);
        await _eventPublisher.PublishAsync(@event, cancellationToken);

        return new CreateFeatureToggleResult(
            Id: toggle.Id,
            FeatureName: toggle.FeatureName,
            IsEnabled: toggle.IsEnabled,
            Scope: toggle.Scope.ToString(),
            CanaryPercentage: toggle.CanaryPercentage);
    }
}
