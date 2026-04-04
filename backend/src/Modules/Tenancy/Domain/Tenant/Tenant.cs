using Muntada.SharedKernel.Domain;
using Muntada.Tenancy.Domain.Events;

namespace Muntada.Tenancy.Domain.Tenant;

/// <summary>
/// Aggregate root representing a tenant (organization/workspace) on the Muntada platform.
/// Manages tenant lifecycle, branding, and billing status with enforced state transitions.
/// </summary>
public class Tenant : AggregateRoot<Guid>
{
    /// <summary>
    /// Gets the display name of the tenant.
    /// </summary>
    public string Name { get; private set; } = default!;

    /// <summary>
    /// Gets the URL-safe slug used for routing and subdomains.
    /// </summary>
    public TenantSlug Slug { get; private set; } = default!;

    /// <summary>
    /// Gets the visual branding configuration for the tenant.
    /// </summary>
    public TenantBranding Branding { get; private set; } = default!;

    /// <summary>
    /// Gets the current lifecycle status of the tenant.
    /// </summary>
    public TenantStatus Status { get; private set; }

    /// <summary>
    /// Gets the current billing/subscription status.
    /// </summary>
    public BillingStatus BillingStatus { get; private set; }

    /// <summary>
    /// Gets the UTC date and time when the tenant's trial period expires, or <c>null</c> if not on trial.
    /// </summary>
    public DateTime? TrialEndsAt { get; private set; }

    /// <summary>
    /// Gets the identifier of the user who created this tenant.
    /// </summary>
    public Guid CreatedBy { get; private set; }

    /// <summary>
    /// Private constructor for EF Core materialization.
    /// </summary>
    private Tenant() { }

    /// <summary>
    /// Creates a new tenant with the specified name and slug, setting the initial status
    /// to <see cref="TenantStatus.Active"/> and billing status to <see cref="BillingStatus.Trial"/>
    /// with a 14-day trial period. Raises a <see cref="TenantCreatedDomainEvent"/>.
    /// </summary>
    /// <param name="name">The display name of the tenant.</param>
    /// <param name="slug">The URL-safe slug for the tenant.</param>
    /// <param name="createdBy">The identifier of the user creating the tenant.</param>
    /// <returns>A new <see cref="Tenant"/> aggregate instance.</returns>
    public static Tenant Create(string name, TenantSlug slug, Guid createdBy)
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug,
            Branding = TenantBranding.Empty,
            Status = TenantStatus.Active,
            BillingStatus = BillingStatus.Trial,
            TrialEndsAt = DateTime.UtcNow.AddDays(14),
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        tenant.AddDomainEvent(new TenantCreatedDomainEvent(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            tenant.Id,
            tenant.Name,
            tenant.Slug.Value));

        return tenant;
    }

    /// <summary>
    /// Updates the tenant's visual branding configuration.
    /// </summary>
    /// <param name="branding">The new branding settings to apply.</param>
    /// <exception cref="InvalidOperationException">Thrown when the tenant is deleted.</exception>
    public void UpdateBranding(TenantBranding branding)
    {
        EnsureNotDeleted();
        Branding = branding;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Suspends the tenant, restricting access until reactivation.
    /// Can only be called when the tenant is currently <see cref="TenantStatus.Active"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the tenant is not in the Active state.</exception>
    public void Suspend()
    {
        if (Status != TenantStatus.Active)
            throw new InvalidOperationException(
                $"Cannot suspend a tenant with status '{Status}'. Only Active tenants can be suspended.");

        Status = TenantStatus.Suspended;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Reactivates a suspended tenant, restoring full access.
    /// Can only be called when the tenant is currently <see cref="TenantStatus.Suspended"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the tenant is not in the Suspended state.</exception>
    public void Activate()
    {
        if (Status != TenantStatus.Suspended)
            throw new InvalidOperationException(
                $"Cannot activate a tenant with status '{Status}'. Only Suspended tenants can be activated.");

        Status = TenantStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Soft-deletes the tenant, marking it for permanent removal according to the retention policy.
    /// Can only be called when the tenant is <see cref="TenantStatus.Active"/> or <see cref="TenantStatus.Suspended"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the tenant is already deleted.</exception>
    public void SoftDelete()
    {
        if (Status == TenantStatus.Deleted)
            throw new InvalidOperationException("Tenant is already deleted.");

        Status = TenantStatus.Deleted;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Ends the trial period by transitioning the billing status.
    /// Used by <c>TrialExpirationJob</c> when the trial period expires.
    /// Data is preserved; only plan limits change.
    /// </summary>
    /// <param name="newBillingStatus">The new billing status (typically Active for Free plan).</param>
    /// <exception cref="InvalidOperationException">Thrown when the tenant is not on trial.</exception>
    public void EndTrial(BillingStatus newBillingStatus)
    {
        if (BillingStatus != BillingStatus.Trial)
            throw new InvalidOperationException("Tenant is not on trial.");

        BillingStatus = newBillingStatus;
        TrialEndsAt = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private void EnsureNotDeleted()
    {
        if (Status == TenantStatus.Deleted)
            throw new InvalidOperationException("Cannot modify a deleted tenant.");
    }
}
