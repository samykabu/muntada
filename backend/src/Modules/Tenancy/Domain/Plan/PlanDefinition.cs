using Muntada.SharedKernel.Domain;

namespace Muntada.Tenancy.Domain.Plan;

/// <summary>
/// Represents a subscription plan definition that tenants can subscribe to.
/// Contains pricing, tier, and usage limits configuration.
/// </summary>
public class PlanDefinition : Entity<Guid>
{
    /// <summary>
    /// Gets the display name of the plan.
    /// </summary>
    public string Name { get; private set; } = default!;

    /// <summary>
    /// Gets the pricing tier of this plan.
    /// </summary>
    public PlanTier Tier { get; private set; }

    /// <summary>
    /// Gets the monthly price in US dollars.
    /// </summary>
    public decimal MonthlyPriceUsd { get; private set; }

    /// <summary>
    /// Gets the usage limits and feature flags associated with this plan.
    /// </summary>
    public PlanLimits Limits { get; private set; } = default!;

    /// <summary>
    /// Gets a value indicating whether this plan definition is currently available for subscription.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Gets the UTC date and time when this plan definition was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the UTC date and time when this plan definition was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Private constructor for EF Core materialization.
    /// </summary>
    private PlanDefinition() { }

    /// <summary>
    /// Creates a new plan definition with the specified configuration.
    /// </summary>
    /// <param name="name">The display name of the plan.</param>
    /// <param name="tier">The pricing tier.</param>
    /// <param name="monthlyPriceUsd">The monthly price in US dollars.</param>
    /// <param name="limits">The usage limits for the plan.</param>
    /// <returns>A new active <see cref="PlanDefinition"/> instance.</returns>
    public static PlanDefinition Create(string name, PlanTier tier, decimal monthlyPriceUsd, PlanLimits limits)
    {
        return new PlanDefinition
        {
            Id = Guid.NewGuid(),
            Name = name,
            Tier = tier,
            MonthlyPriceUsd = monthlyPriceUsd,
            Limits = limits,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
