using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Muntada.Tenancy.Application.Commands;
using Muntada.Tenancy.Application.Services;
using Muntada.Tenancy.Domain.Features;

namespace Muntada.Tenancy.Api.Controllers;

/// <summary>
/// Response DTO representing a feature toggle's configuration.
/// </summary>
/// <param name="Id">The unique identifier of the feature toggle.</param>
/// <param name="FeatureName">The name of the feature toggle.</param>
/// <param name="IsEnabled">Whether the toggle is globally enabled.</param>
/// <param name="Scope">The evaluation scope of the toggle.</param>
/// <param name="CanaryPercentage">The canary rollout percentage (0-100).</param>
/// <param name="Overrides">The per-tenant overrides for this toggle.</param>
public sealed record FeatureToggleResponse(
    Guid Id,
    string FeatureName,
    bool IsEnabled,
    string Scope,
    int CanaryPercentage,
    IReadOnlyList<FeatureToggleOverrideResponse> Overrides);

/// <summary>
/// Response DTO representing a per-tenant feature toggle override.
/// </summary>
/// <param name="TenantId">The identifier of the tenant.</param>
/// <param name="IsEnabled">Whether the feature is enabled for this tenant.</param>
public sealed record FeatureToggleOverrideResponse(
    Guid TenantId,
    bool IsEnabled);

/// <summary>
/// Request DTO for creating a new feature toggle.
/// </summary>
/// <param name="FeatureName">The unique name identifying the feature toggle.</param>
/// <param name="Scope">The evaluation scope (Global, PerTenant, Canary, etc.).</param>
public sealed record CreateFeatureToggleRequest(
    string FeatureName,
    string Scope);

/// <summary>
/// Request DTO for updating an existing feature toggle.
/// </summary>
/// <param name="IsEnabled">Optional new enabled state, or <c>null</c> to keep current.</param>
/// <param name="Scope">Optional new scope, or <c>null</c> to keep current.</param>
/// <param name="CanaryPercentage">Optional new canary percentage (0-100), or <c>null</c> to keep current.</param>
/// <param name="AddOverrides">Optional list of per-tenant overrides to add.</param>
/// <param name="RemoveOverrideTenantIds">Optional list of tenant IDs whose overrides should be removed.</param>
public sealed record UpdateFeatureToggleRequest(
    bool? IsEnabled,
    string? Scope,
    int? CanaryPercentage,
    List<FeatureToggleOverrideInputDto>? AddOverrides,
    List<Guid>? RemoveOverrideTenantIds);

/// <summary>
/// Input DTO for adding a per-tenant feature toggle override.
/// </summary>
/// <param name="TenantId">The identifier of the tenant.</param>
/// <param name="IsEnabled">Whether the feature should be enabled for this tenant.</param>
public sealed record FeatureToggleOverrideInputDto(Guid TenantId, bool IsEnabled);

/// <summary>
/// Response DTO for the list of enabled features for a tenant.
/// </summary>
/// <param name="TenantId">The identifier of the tenant.</param>
/// <param name="EnabledFeatures">The list of enabled feature names.</param>
public sealed record EnabledFeaturesResponse(
    Guid TenantId,
    IReadOnlyList<string> EnabledFeatures);

/// <summary>
/// Handles admin CRUD operations for feature toggles, per-tenant overrides,
/// and querying enabled features for a specific tenant.
/// </summary>
[ApiController]
[Route("api/v1/feature-toggles")]
public sealed class FeatureTogglesController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IFeatureToggleService _featureToggleService;

    /// <summary>
    /// Initializes a new instance of <see cref="FeatureTogglesController"/>.
    /// </summary>
    /// <param name="sender">MediatR sender for dispatching commands and queries.</param>
    /// <param name="featureToggleService">Service for evaluating feature toggle state.</param>
    public FeatureTogglesController(
        ISender sender,
        IFeatureToggleService featureToggleService)
    {
        _sender = sender;
        _featureToggleService = featureToggleService;
    }

    /// <summary>
    /// Creates a new feature toggle. The toggle is disabled by default.
    /// </summary>
    /// <param name="request">The feature toggle creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>201 Created with the new toggle's details.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(FeatureToggleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateToggle(
        [FromBody] CreateFeatureToggleRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<FeatureToggleScope>(request.Scope, true, out var scope))
        {
            return BadRequest(new { error = $"Invalid scope '{request.Scope}'. Valid values: {string.Join(", ", Enum.GetNames<FeatureToggleScope>())}" });
        }

        var command = new CreateFeatureToggleCommand(request.FeatureName, scope);
        var result = await _sender.Send(command, cancellationToken);

        var response = new FeatureToggleResponse(
            Id: result.Id,
            FeatureName: result.FeatureName,
            IsEnabled: result.IsEnabled,
            Scope: result.Scope,
            CanaryPercentage: result.CanaryPercentage,
            Overrides: []);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>
    /// Updates an existing feature toggle's state, scope, canary percentage,
    /// and manages per-tenant overrides.
    /// </summary>
    /// <param name="id">The identifier of the feature toggle to update.</param>
    /// <param name="request">The toggle update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the updated toggle details.</returns>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(FeatureToggleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateToggle(
        Guid id,
        [FromBody] UpdateFeatureToggleRequest request,
        CancellationToken cancellationToken)
    {
        var addOverrides = request.AddOverrides?
            .Select(o => new FeatureToggleOverrideInput(o.TenantId, o.IsEnabled))
            .ToList();

        var command = new UpdateFeatureToggleCommand(
            id,
            request.IsEnabled,
            request.Scope is not null && Enum.TryParse<FeatureToggleScope>(request.Scope, true, out var scope) ? scope : null,
            request.CanaryPercentage,
            addOverrides,
            request.RemoveOverrideTenantIds);

        var result = await _sender.Send(command, cancellationToken);

        var response = new FeatureToggleResponse(
            Id: result.Id,
            FeatureName: result.FeatureName,
            IsEnabled: result.IsEnabled,
            Scope: result.Scope,
            CanaryPercentage: result.CanaryPercentage,
            Overrides: result.Overrides
                .Select(o => new FeatureToggleOverrideResponse(o.TenantId, o.IsEnabled))
                .ToList());

        return Ok(response);
    }

    /// <summary>
    /// Retrieves the list of feature names that are currently enabled for a specific tenant.
    /// Evaluates global state, per-tenant overrides, and canary percentage.
    /// </summary>
    /// <param name="tenantId">The identifier of the tenant.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the list of enabled feature names.</returns>
    [HttpGet("tenants/{tenantId:guid}/enabled")]
    [ProducesResponseType(typeof(EnabledFeaturesResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEnabledFeatures(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var enabledFeatures = await _featureToggleService.GetEnabledFeaturesAsync(
            tenantId, cancellationToken);

        return Ok(new EnabledFeaturesResponse(tenantId, enabledFeatures));
    }
}
