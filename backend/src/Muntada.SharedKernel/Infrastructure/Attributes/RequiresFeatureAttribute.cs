namespace Muntada.SharedKernel.Infrastructure.Attributes;

/// <summary>
/// Marks a controller or action as requiring a specific feature toggle to be enabled.
/// When applied, the <c>FeatureToggleMiddleware</c> will verify that the feature is
/// enabled for the current tenant before allowing the request to proceed.
/// Returns HTTP 403 Forbidden if the feature is disabled.
/// </summary>
/// <remarks>
/// Can be applied to controllers (all actions require the feature) or individual actions.
/// Multiple attributes can be stacked to require multiple features.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequiresFeatureAttribute : Attribute
{
    /// <summary>
    /// Gets the name of the required feature toggle.
    /// </summary>
    public string FeatureName { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="RequiresFeatureAttribute"/>
    /// with the specified feature name.
    /// </summary>
    /// <param name="featureName">The name of the feature toggle that must be enabled.</param>
    public RequiresFeatureAttribute(string featureName)
    {
        FeatureName = featureName;
    }
}
