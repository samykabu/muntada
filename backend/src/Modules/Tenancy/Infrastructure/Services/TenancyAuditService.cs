using Microsoft.Extensions.Logging;

namespace Muntada.Tenancy.Infrastructure.Services;

/// <summary>
/// Provides structured audit logging for state changes in the Tenancy module.
/// Logs changes to tenants, memberships, plans, retention policies, and feature toggles
/// using structured properties for downstream consumption by log aggregation systems.
/// </summary>
/// <remarks>
/// This service currently writes to the application log pipeline via <see cref="ILogger"/>.
/// It can be extended to persist audit entries to the <c>AuditLog</c> entity in the database.
/// </remarks>
public class TenancyAuditService
{
    private readonly ILogger<TenancyAuditService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="TenancyAuditService"/>.
    /// </summary>
    /// <param name="logger">The logger instance for structured audit output.</param>
    public TenancyAuditService(ILogger<TenancyAuditService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Logs an audit entry for tenant creation.
    /// </summary>
    /// <param name="tenantId">The identifier of the created tenant.</param>
    /// <param name="tenantName">The display name of the tenant.</param>
    /// <param name="slug">The URL-safe slug assigned to the tenant.</param>
    /// <param name="createdBy">The identifier of the user who created the tenant.</param>
    public void LogTenantCreated(Guid tenantId, string tenantName, string slug, Guid createdBy)
    {
        _logger.LogInformation(
            "Audit: Tenant created — TenantId={TenantId}, Name={TenantName}, Slug={Slug}, CreatedBy={CreatedBy}",
            tenantId, tenantName, slug, createdBy);
    }

    /// <summary>
    /// Logs an audit entry for membership changes (invite, accept, role update, removal).
    /// </summary>
    /// <param name="tenantId">The identifier of the tenant.</param>
    /// <param name="membershipId">The identifier of the membership.</param>
    /// <param name="changeType">The type of change (e.g., "Invited", "Accepted", "RoleUpdated", "Removed").</param>
    /// <param name="performedBy">The identifier of the user who performed the change.</param>
    /// <param name="details">Optional additional details about the change.</param>
    public void LogMembershipChanged(
        Guid tenantId,
        Guid membershipId,
        string changeType,
        Guid performedBy,
        string? details = null)
    {
        _logger.LogInformation(
            "Audit: Membership changed — TenantId={TenantId}, MembershipId={MembershipId}, ChangeType={ChangeType}, PerformedBy={PerformedBy}, Details={Details}",
            tenantId, membershipId, changeType, performedBy, details);
    }

    /// <summary>
    /// Logs an audit entry for plan changes (assignment, upgrade, downgrade).
    /// </summary>
    /// <param name="tenantId">The identifier of the tenant.</param>
    /// <param name="previousPlan">The name of the previous plan, or <c>null</c> for initial assignment.</param>
    /// <param name="newPlan">The name of the new plan.</param>
    /// <param name="changeType">The type of change (e.g., "Assigned", "Upgraded", "Downgraded").</param>
    /// <param name="performedBy">The identifier of the user who performed the change.</param>
    public void LogPlanChanged(
        Guid tenantId,
        string? previousPlan,
        string newPlan,
        string changeType,
        Guid performedBy)
    {
        _logger.LogInformation(
            "Audit: Plan changed — TenantId={TenantId}, PreviousPlan={PreviousPlan}, NewPlan={NewPlan}, ChangeType={ChangeType}, PerformedBy={PerformedBy}",
            tenantId, previousPlan, newPlan, changeType, performedBy);
    }

    /// <summary>
    /// Logs an audit entry for retention policy updates.
    /// </summary>
    /// <param name="tenantId">The identifier of the tenant.</param>
    /// <param name="updatedFields">A summary of which retention fields were changed.</param>
    public void LogRetentionUpdated(Guid tenantId, string updatedFields)
    {
        _logger.LogInformation(
            "Audit: Retention policy updated — TenantId={TenantId}, UpdatedFields={UpdatedFields}",
            tenantId, updatedFields);
    }

    /// <summary>
    /// Logs an audit entry for feature toggle changes (creation, enable/disable, override).
    /// </summary>
    /// <param name="featureName">The name of the feature toggle.</param>
    /// <param name="changeType">The type of change (e.g., "Created", "Enabled", "Disabled", "OverrideAdded", "OverrideRemoved").</param>
    /// <param name="tenantId">The tenant identifier, if the change is tenant-scoped; otherwise <c>null</c>.</param>
    public void LogFeatureToggleChanged(string featureName, string changeType, Guid? tenantId = null)
    {
        _logger.LogInformation(
            "Audit: Feature toggle changed — FeatureName={FeatureName}, ChangeType={ChangeType}, TenantId={TenantId}",
            featureName, changeType, tenantId);
    }
}
