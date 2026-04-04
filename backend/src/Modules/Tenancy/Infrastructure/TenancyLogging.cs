using Microsoft.Extensions.Logging;

namespace Muntada.Tenancy.Infrastructure;

/// <summary>
/// Provides high-performance structured logging definitions for the Tenancy module
/// using <c>LoggerMessage.Define</c>. Each definition uses a compile-time generated
/// delegate to avoid boxing and string formatting overhead at runtime.
/// </summary>
/// <remarks>
/// Event ID ranges for Tenancy module: 2000-2099.
/// Usage: inject <see cref="ILogger"/> and call the static action delegates, e.g.
/// <c>TenancyLogging.TenantCreated(logger, tenantId, slug, null);</c>
/// </remarks>
public static class TenancyLogging
{
    // ──────────────────────────────────────────────────────
    // Tenant lifecycle (2000-2009)
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// Logs that a new tenant was created.
    /// </summary>
    public static readonly Action<ILogger, Guid, string, Exception?> TenantCreated =
        LoggerMessage.Define<Guid, string>(
            LogLevel.Information,
            new EventId(2000, nameof(TenantCreated)),
            "Tenant created: TenantId={TenantId}, Slug={Slug}");

    /// <summary>
    /// Logs that a tenant's trial period has expired.
    /// </summary>
    public static readonly Action<ILogger, Guid, string, Exception?> TrialExpired =
        LoggerMessage.Define<Guid, string>(
            LogLevel.Warning,
            new EventId(2001, nameof(TrialExpired)),
            "Trial expired for tenant: TenantId={TenantId}, NewBillingStatus={NewBillingStatus}");

    /// <summary>
    /// Logs that a tenant has been suspended.
    /// </summary>
    public static readonly Action<ILogger, Guid, Exception?> TenantSuspended =
        LoggerMessage.Define<Guid>(
            LogLevel.Warning,
            new EventId(2002, nameof(TenantSuspended)),
            "Tenant suspended: TenantId={TenantId}");

    // ──────────────────────────────────────────────────────
    // Membership (2010-2019)
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// Logs that a member was invited to a tenant.
    /// </summary>
    public static readonly Action<ILogger, Guid, string, string, Exception?> MemberInvited =
        LoggerMessage.Define<Guid, string, string>(
            LogLevel.Information,
            new EventId(2010, nameof(MemberInvited)),
            "Member invited: TenantId={TenantId}, Email={Email}, Role={Role}");

    /// <summary>
    /// Logs that a member accepted a tenant invitation.
    /// </summary>
    public static readonly Action<ILogger, Guid, Guid, Exception?> MemberAccepted =
        LoggerMessage.Define<Guid, Guid>(
            LogLevel.Information,
            new EventId(2011, nameof(MemberAccepted)),
            "Member accepted invite: TenantId={TenantId}, UserId={UserId}");

    // ──────────────────────────────────────────────────────
    // Plans (2020-2029)
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// Logs that a plan was assigned to a tenant.
    /// </summary>
    public static readonly Action<ILogger, Guid, string, Exception?> PlanAssigned =
        LoggerMessage.Define<Guid, string>(
            LogLevel.Information,
            new EventId(2020, nameof(PlanAssigned)),
            "Plan assigned: TenantId={TenantId}, PlanName={PlanName}");

    /// <summary>
    /// Logs that a tenant upgraded their plan.
    /// </summary>
    public static readonly Action<ILogger, Guid, string, string, Exception?> PlanUpgraded =
        LoggerMessage.Define<Guid, string, string>(
            LogLevel.Information,
            new EventId(2021, nameof(PlanUpgraded)),
            "Plan upgraded: TenantId={TenantId}, From={OldPlan}, To={NewPlan}");

    /// <summary>
    /// Logs that a tenant downgraded their plan.
    /// </summary>
    public static readonly Action<ILogger, Guid, string, string, Exception?> PlanDowngraded =
        LoggerMessage.Define<Guid, string, string>(
            LogLevel.Information,
            new EventId(2022, nameof(PlanDowngraded)),
            "Plan downgraded: TenantId={TenantId}, From={OldPlan}, To={NewPlan}");

    // ──────────────────────────────────────────────────────
    // Retention (2030-2039)
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// Logs that a tenant's retention policy was updated.
    /// </summary>
    public static readonly Action<ILogger, Guid, Exception?> RetentionPolicyUpdated =
        LoggerMessage.Define<Guid>(
            LogLevel.Information,
            new EventId(2030, nameof(RetentionPolicyUpdated)),
            "Retention policy updated: TenantId={TenantId}");

    // ──────────────────────────────────────────────────────
    // Feature toggles (2040-2049)
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// Logs that a feature toggle was changed (created, enabled, disabled, or override modified).
    /// </summary>
    public static readonly Action<ILogger, string, bool, Exception?> FeatureToggleChanged =
        LoggerMessage.Define<string, bool>(
            LogLevel.Information,
            new EventId(2040, nameof(FeatureToggleChanged)),
            "Feature toggle changed: FeatureName={FeatureName}, IsEnabled={IsEnabled}");

    // ──────────────────────────────────────────────────────
    // Usage & limits (2050-2059)
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// Logs that a tenant is approaching a usage limit (80% or above).
    /// </summary>
    public static readonly Action<ILogger, Guid, string, long, long, Exception?> UsageLimitApproached =
        LoggerMessage.Define<Guid, string, long, long>(
            LogLevel.Warning,
            new EventId(2050, nameof(UsageLimitApproached)),
            "Usage limit approaching: TenantId={TenantId}, Resource={Resource}, Current={CurrentUsage}, Limit={Limit}");

    /// <summary>
    /// Logs that a tenant has exceeded a usage limit.
    /// </summary>
    public static readonly Action<ILogger, Guid, string, long, long, Exception?> UsageLimitExceeded =
        LoggerMessage.Define<Guid, string, long, long>(
            LogLevel.Error,
            new EventId(2051, nameof(UsageLimitExceeded)),
            "Usage limit exceeded: TenantId={TenantId}, Resource={Resource}, Current={CurrentUsage}, Limit={Limit}");
}
