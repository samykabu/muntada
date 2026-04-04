using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Muntada.Tenancy.Application.Services;
using Muntada.Tenancy.Domain.Features;

namespace Muntada.Tenancy.Infrastructure.Services;

/// <summary>
/// Implements <see cref="IFeatureToggleService"/> by evaluating feature toggle scopes:
/// global state, per-tenant overrides, and canary percentage-based rollout.
/// </summary>
/// <remarks>
/// Evaluation order:
/// 1. Per-tenant override (if exists) takes precedence over all other rules.
/// 2. Canary scope uses a deterministic hash of tenant ID to percentage for consistent rollout.
/// 3. Global scope returns the global <c>IsEnabled</c> state.
///
/// TODO: Add Redis caching layer for production performance.
/// </remarks>
public class FeatureToggleService : IFeatureToggleService
{
    private readonly TenancyDbContext _dbContext;
    private readonly ILogger<FeatureToggleService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="FeatureToggleService"/>.
    /// </summary>
    /// <param name="dbContext">The Tenancy module database context.</param>
    /// <param name="logger">Logger for feature toggle evaluations.</param>
    public FeatureToggleService(
        TenancyDbContext dbContext,
        ILogger<FeatureToggleService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> IsFeatureEnabledAsync(
        string featureName,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        // TODO: Check Redis cache first, fallback to DB
        var toggle = await _dbContext.FeatureToggles
            .AsNoTracking()
            .Include(f => f.Overrides)
            .FirstOrDefaultAsync(f => f.FeatureName == featureName, cancellationToken);

        if (toggle is null)
        {
            _logger.LogDebug("Feature toggle '{FeatureName}' not found, returning disabled", featureName);
            return false;
        }

        return EvaluateToggle(toggle, tenantId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetEnabledFeaturesAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        // TODO: Check Redis cache first, fallback to DB
        var toggles = await _dbContext.FeatureToggles
            .AsNoTracking()
            .Include(f => f.Overrides)
            .ToListAsync(cancellationToken);

        var enabledFeatures = new List<string>();

        foreach (var toggle in toggles)
        {
            if (EvaluateToggle(toggle, tenantId))
            {
                enabledFeatures.Add(toggle.FeatureName);
            }
        }

        return enabledFeatures.AsReadOnly();
    }

    /// <summary>
    /// Evaluates a feature toggle for a specific tenant using the configured scope logic.
    /// </summary>
    /// <param name="toggle">The feature toggle to evaluate.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <returns><c>true</c> if the feature is enabled for the tenant.</returns>
    private bool EvaluateToggle(FeatureToggle toggle, Guid tenantId)
    {
        // 1. Check per-tenant override first (takes precedence)
        var tenantOverride = toggle.Overrides.FirstOrDefault(o => o.TenantId == tenantId);
        if (tenantOverride is not null)
        {
            _logger.LogDebug(
                "Feature '{FeatureName}' has tenant override for {TenantId}: {Enabled}",
                toggle.FeatureName, tenantId, tenantOverride.IsEnabled);
            return tenantOverride.IsEnabled;
        }

        // 2. If not globally enabled, feature is off
        if (!toggle.IsEnabled)
            return false;

        // 3. Evaluate scope-specific logic
        return toggle.Scope switch
        {
            FeatureToggleScope.Global => true,
            FeatureToggleScope.PerTenant => true, // Enabled globally means enabled for all tenants
            FeatureToggleScope.Canary => IsInCanaryPercentage(tenantId, toggle.CanaryPercentage),
            _ => true // Default: if enabled globally, treat as enabled
        };
    }

    /// <summary>
    /// Determines whether a tenant falls within the canary rollout percentage
    /// using a deterministic hash for consistent assignment.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="percentage">The canary percentage (0-100).</param>
    /// <returns><c>true</c> if the tenant is in the canary group.</returns>
    private static bool IsInCanaryPercentage(Guid tenantId, int percentage)
    {
        if (percentage >= 100) return true;
        if (percentage <= 0) return false;

        // Use a stable hash from GUID bytes for consistent canary assignment
        var bytes = tenantId.ToByteArray();
        var hash = BitConverter.ToUInt32(bytes, 0);
        var bucket = (int)(hash % 100);
        return bucket < percentage;
    }
}
