using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.SharedKernel.Application;
using Muntada.Tenancy.Domain.Events;
using Muntada.Tenancy.Domain.Features;
using Muntada.Tenancy.Infrastructure;

namespace Muntada.Tenancy.Application.Commands;

/// <summary>
/// Command to update an existing feature toggle's state, scope, canary percentage,
/// and manage per-tenant overrides.
/// </summary>
/// <param name="FeatureToggleId">The identifier of the feature toggle to update.</param>
/// <param name="IsEnabled">Optional new enabled state, or <c>null</c> to keep current.</param>
/// <param name="Scope">Optional new scope, or <c>null</c> to keep current.</param>
/// <param name="CanaryPercentage">Optional new canary percentage (0-100), or <c>null</c> to keep current.</param>
/// <param name="AddOverrides">Optional list of per-tenant overrides to add.</param>
/// <param name="RemoveOverrideTenantIds">Optional list of tenant IDs whose overrides should be removed.</param>
public sealed record UpdateFeatureToggleCommand(
    Guid FeatureToggleId,
    bool? IsEnabled,
    FeatureToggleScope? Scope,
    int? CanaryPercentage,
    IReadOnlyList<FeatureToggleOverrideInput>? AddOverrides,
    IReadOnlyList<Guid>? RemoveOverrideTenantIds) : IRequest<UpdateFeatureToggleResult>;

/// <summary>
/// Input for adding a per-tenant feature toggle override.
/// </summary>
/// <param name="TenantId">The identifier of the tenant.</param>
/// <param name="IsEnabled">Whether the feature should be enabled for this tenant.</param>
public sealed record FeatureToggleOverrideInput(Guid TenantId, bool IsEnabled);

/// <summary>
/// Result returned after a successful feature toggle update.
/// </summary>
/// <param name="Id">The identifier of the updated toggle.</param>
/// <param name="FeatureName">The name of the feature toggle.</param>
/// <param name="IsEnabled">Whether the toggle is globally enabled.</param>
/// <param name="Scope">The evaluation scope of the toggle.</param>
/// <param name="CanaryPercentage">The canary rollout percentage.</param>
/// <param name="Overrides">The current per-tenant overrides.</param>
public sealed record UpdateFeatureToggleResult(
    Guid Id,
    string FeatureName,
    bool IsEnabled,
    string Scope,
    int CanaryPercentage,
    IReadOnlyList<FeatureToggleOverrideResult> Overrides);

/// <summary>
/// Result representing a per-tenant feature toggle override.
/// </summary>
/// <param name="TenantId">The identifier of the tenant.</param>
/// <param name="IsEnabled">Whether the feature is enabled for this tenant.</param>
public sealed record FeatureToggleOverrideResult(Guid TenantId, bool IsEnabled);

/// <summary>
/// Handles <see cref="UpdateFeatureToggleCommand"/> by applying state changes, managing
/// per-tenant overrides, and publishing a <see cref="FeatureToggleChangedEvent"/>.
/// </summary>
public sealed class UpdateFeatureToggleCommandHandler
    : IRequestHandler<UpdateFeatureToggleCommand, UpdateFeatureToggleResult>
{
    private readonly TenancyDbContext _dbContext;
    private readonly IIntegrationEventPublisher _eventPublisher;

    /// <summary>
    /// Initializes a new instance of <see cref="UpdateFeatureToggleCommandHandler"/>.
    /// </summary>
    /// <param name="dbContext">The Tenancy module database context.</param>
    /// <param name="eventPublisher">Publisher for integration events.</param>
    public UpdateFeatureToggleCommandHandler(
        TenancyDbContext dbContext,
        IIntegrationEventPublisher eventPublisher)
    {
        _dbContext = dbContext;
        _eventPublisher = eventPublisher;
    }

    /// <summary>
    /// Handles the toggle update: finds the toggle, applies state changes and overrides,
    /// publishes the <see cref="FeatureToggleChangedEvent"/>, and persists changes.
    /// </summary>
    /// <param name="request">The feature toggle update command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated feature toggle details.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the feature toggle is not found.
    /// </exception>
    public async Task<UpdateFeatureToggleResult> Handle(
        UpdateFeatureToggleCommand request,
        CancellationToken cancellationToken)
    {
        var toggle = await _dbContext.FeatureToggles
            .Include(f => f.Overrides)
            .FirstOrDefaultAsync(f => f.Id == request.FeatureToggleId, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Feature toggle '{request.FeatureToggleId}' not found.");

        // Apply state changes
        if (request.IsEnabled.HasValue)
        {
            if (request.IsEnabled.Value)
                toggle.Enable();
            else
                toggle.Disable();
        }

        if (request.CanaryPercentage.HasValue)
        {
            toggle.SetCanaryPercentage(request.CanaryPercentage.Value);
        }

        // Remove overrides first (to allow re-adding with different state)
        if (request.RemoveOverrideTenantIds is { Count: > 0 })
        {
            foreach (var tenantId in request.RemoveOverrideTenantIds)
            {
                toggle.RemoveOverride(tenantId);
            }
        }

        // Add new overrides
        if (request.AddOverrides is { Count: > 0 })
        {
            foreach (var overrideInput in request.AddOverrides)
            {
                toggle.AddOverride(overrideInput.TenantId, overrideInput.IsEnabled);
            }
        }

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

        return new UpdateFeatureToggleResult(
            Id: toggle.Id,
            FeatureName: toggle.FeatureName,
            IsEnabled: toggle.IsEnabled,
            Scope: toggle.Scope.ToString(),
            CanaryPercentage: toggle.CanaryPercentage,
            Overrides: toggle.Overrides
                .Select(o => new FeatureToggleOverrideResult(o.TenantId, o.IsEnabled))
                .ToList());
    }
}
