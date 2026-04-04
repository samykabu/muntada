using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Muntada.SharedKernel.Infrastructure.Attributes;
using Muntada.Tenancy.Application.Services;

namespace Muntada.Tenancy.Infrastructure.Middleware;

/// <summary>
/// Middleware that enforces feature toggle requirements on controllers and actions.
/// Inspects <see cref="RequiresFeatureAttribute"/> decorations and returns HTTP 403
/// if the required feature is not enabled for the current tenant.
/// </summary>
public class FeatureToggleMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of <see cref="FeatureToggleMiddleware"/>.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    public FeatureToggleMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Inspects the current endpoint for <see cref="RequiresFeatureAttribute"/> decorations.
    /// If present, evaluates each required feature against the current tenant context.
    /// Returns 403 Forbidden if any required feature is disabled.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="featureToggleService">The feature toggle evaluation service.</param>
    /// <param name="tenantContext">The current tenant context.</param>
    public async Task InvokeAsync(
        HttpContext context,
        IFeatureToggleService featureToggleService,
        ITenantContext tenantContext)
    {
        var endpoint = context.GetEndpoint();

        if (endpoint is null)
        {
            await _next(context);
            return;
        }

        // Collect RequiresFeature attributes from both action and controller
        var requiredFeatures = GetRequiredFeatures(endpoint);

        if (requiredFeatures.Count == 0)
        {
            await _next(context);
            return;
        }

        // If no tenant context, skip feature check (let auth middleware handle it)
        if (!tenantContext.HasTenant)
        {
            await _next(context);
            return;
        }

        foreach (var featureName in requiredFeatures)
        {
            var isEnabled = await featureToggleService.IsFeatureEnabledAsync(
                featureName, tenantContext.TenantId);

            if (!isEnabled)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(new
                {
                    type = "https://muntada.com/errors/feature-disabled",
                    title = "Feature not available",
                    status = 403,
                    detail = $"The feature '{featureName}' is not enabled for your organization.",
                    code = "FEATURE_DISABLED",
                    featureName
                });
                return;
            }
        }

        await _next(context);
    }

    /// <summary>
    /// Extracts all <see cref="RequiresFeatureAttribute"/> feature names from the endpoint metadata,
    /// including both action-level and controller-level attributes.
    /// </summary>
    /// <param name="endpoint">The current endpoint.</param>
    /// <returns>A list of required feature names.</returns>
    private static List<string> GetRequiredFeatures(Endpoint endpoint)
    {
        var features = new List<string>();

        // Get attributes from endpoint metadata (covers action-level attributes)
        var actionAttributes = endpoint.Metadata.GetOrderedMetadata<RequiresFeatureAttribute>();
        features.AddRange(actionAttributes.Select(a => a.FeatureName));

        // Get controller-level attributes via ControllerActionDescriptor
        var controllerDescriptor = endpoint.Metadata
            .GetMetadata<ControllerActionDescriptor>();

        if (controllerDescriptor is not null)
        {
            var controllerAttributes = controllerDescriptor.ControllerTypeInfo
                .GetCustomAttributes(typeof(RequiresFeatureAttribute), true)
                .Cast<RequiresFeatureAttribute>();

            foreach (var attr in controllerAttributes)
            {
                if (!features.Contains(attr.FeatureName))
                    features.Add(attr.FeatureName);
            }
        }

        return features;
    }
}
