using Muntada.SharedKernel.Domain;

namespace Muntada.Tenancy.Domain.Events;

// ────────────────────────────────────────────────────────────────
// Domain Events
// ────────────────────────────────────────────────────────────────

/// <summary>
/// Domain event raised when a new tenant is created.
/// </summary>
/// <param name="EventId">The unique identifier for this event instance.</param>
/// <param name="OccurredAt">The UTC timestamp when this event occurred.</param>
/// <param name="TenantId">The identifier of the newly created tenant.</param>
/// <param name="TenantName">The display name of the tenant.</param>
/// <param name="Slug">The URL-safe slug assigned to the tenant.</param>
public sealed record TenantCreatedDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid TenantId,
    string TenantName,
    string Slug) : IDomainEvent;

/// <summary>
/// Domain event raised when a tenant membership is changed
/// (created, accepted, role updated, or deactivated).
/// </summary>
/// <param name="EventId">The unique identifier for this event instance.</param>
/// <param name="OccurredAt">The UTC timestamp when this event occurred.</param>
/// <param name="TenantId">The identifier of the tenant whose membership changed.</param>
/// <param name="MembershipId">The identifier of the affected membership.</param>
/// <param name="ChangeType">A descriptor of the change type (e.g. "Invited", "Accepted", "RoleUpdated", "Deactivated").</param>
public sealed record MembershipChangedDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid TenantId,
    Guid MembershipId,
    string ChangeType) : IDomainEvent;

// ────────────────────────────────────────────────────────────────
// Integration Events
// ────────────────────────────────────────────────────────────────

/// <summary>
/// Integration event published when a new tenant is created.
/// Consumed by other modules to provision tenant-scoped resources.
/// </summary>
/// <param name="EventId">The unique identifier for this event instance.</param>
/// <param name="OccurredAt">The UTC timestamp when this event occurred.</param>
/// <param name="AggregateId">The identifier of the source tenant aggregate.</param>
/// <param name="AggregateType">The type name of the source aggregate.</param>
/// <param name="Version">The schema version of this event.</param>
/// <param name="TenantId">The identifier of the newly created tenant.</param>
/// <param name="TenantName">The display name of the tenant.</param>
/// <param name="Slug">The URL-safe slug assigned to the tenant.</param>
/// <param name="CreatedBy">The identifier of the user who created the tenant.</param>
public sealed record TenantCreatedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string AggregateId,
    string AggregateType,
    int Version,
    Guid TenantId,
    string TenantName,
    string Slug,
    Guid CreatedBy) : IIntegrationEvent;

/// <summary>
/// Integration event published when a tenant membership is created, accepted, updated, or deactivated.
/// Consumed by modules that need to react to membership changes (e.g. permissions, notifications).
/// </summary>
/// <param name="EventId">The unique identifier for this event instance.</param>
/// <param name="OccurredAt">The UTC timestamp when this event occurred.</param>
/// <param name="AggregateId">The identifier of the source tenant aggregate.</param>
/// <param name="AggregateType">The type name of the source aggregate.</param>
/// <param name="Version">The schema version of this event.</param>
/// <param name="TenantId">The identifier of the tenant.</param>
/// <param name="MembershipId">The identifier of the affected membership.</param>
/// <param name="UserId">The identifier of the user, or <c>null</c> if pending.</param>
/// <param name="Role">The role assigned to the membership.</param>
/// <param name="ChangeType">A descriptor of the change type (e.g. "Invited", "Accepted", "RoleUpdated", "Deactivated").</param>
public sealed record TenantMembershipChangedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string AggregateId,
    string AggregateType,
    int Version,
    Guid TenantId,
    Guid MembershipId,
    Guid? UserId,
    string Role,
    string ChangeType) : IIntegrationEvent;

/// <summary>
/// Integration event published when a tenant's subscription plan is changed.
/// Consumed by modules that enforce plan-based feature gating and usage limits.
/// </summary>
/// <param name="EventId">The unique identifier for this event instance.</param>
/// <param name="OccurredAt">The UTC timestamp when this event occurred.</param>
/// <param name="AggregateId">The identifier of the source tenant aggregate.</param>
/// <param name="AggregateType">The type name of the source aggregate.</param>
/// <param name="Version">The schema version of this event.</param>
/// <param name="TenantId">The identifier of the tenant.</param>
/// <param name="PlanDefinitionId">The identifier of the new plan definition.</param>
/// <param name="PlanName">The display name of the new plan.</param>
/// <param name="Tier">The pricing tier of the new plan.</param>
public sealed record PlanChangedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string AggregateId,
    string AggregateType,
    int Version,
    Guid TenantId,
    Guid PlanDefinitionId,
    string PlanName,
    string Tier) : IIntegrationEvent;

/// <summary>
/// Integration event published when a tenant exceeds a usage limit defined by their plan.
/// Consumed by billing and notification modules to enforce quotas and alert administrators.
/// </summary>
/// <param name="EventId">The unique identifier for this event instance.</param>
/// <param name="OccurredAt">The UTC timestamp when this event occurred.</param>
/// <param name="AggregateId">The identifier of the source tenant aggregate.</param>
/// <param name="AggregateType">The type name of the source aggregate.</param>
/// <param name="Version">The schema version of this event.</param>
/// <param name="TenantId">The identifier of the tenant.</param>
/// <param name="LimitName">The name of the limit that was exceeded (e.g. "MaxRoomsPerMonth").</param>
/// <param name="CurrentUsage">The current usage value at the time the limit was exceeded.</param>
/// <param name="MaxAllowed">The maximum allowed value defined by the plan.</param>
public sealed record UsageLimitExceededEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string AggregateId,
    string AggregateType,
    int Version,
    Guid TenantId,
    string LimitName,
    long CurrentUsage,
    long MaxAllowed) : IIntegrationEvent;

/// <summary>
/// Integration event published when a tenant's data retention policy is updated.
/// Consumed by modules responsible for data lifecycle management and purging.
/// </summary>
/// <param name="EventId">The unique identifier for this event instance.</param>
/// <param name="OccurredAt">The UTC timestamp when this event occurred.</param>
/// <param name="AggregateId">The identifier of the source tenant aggregate.</param>
/// <param name="AggregateType">The type name of the source aggregate.</param>
/// <param name="Version">The schema version of this event.</param>
/// <param name="TenantId">The identifier of the tenant.</param>
/// <param name="RecordingRetentionDays">The updated recording retention period in days.</param>
/// <param name="ChatMessageRetentionDays">The updated chat message retention period in days.</param>
/// <param name="FileRetentionDays">The updated file retention period in days.</param>
/// <param name="AuditLogRetentionDays">The updated audit log retention period in days.</param>
/// <param name="UserActivityLogRetentionDays">The updated user activity log retention period in days.</param>
public sealed record RetentionPolicyChangedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string AggregateId,
    string AggregateType,
    int Version,
    Guid TenantId,
    int RecordingRetentionDays,
    int ChatMessageRetentionDays,
    int FileRetentionDays,
    int AuditLogRetentionDays,
    int UserActivityLogRetentionDays) : IIntegrationEvent;

/// <summary>
/// Integration event published when a feature toggle's configuration is changed.
/// Consumed by modules that cache feature flag state and need to invalidate on changes.
/// </summary>
/// <param name="EventId">The unique identifier for this event instance.</param>
/// <param name="OccurredAt">The UTC timestamp when this event occurred.</param>
/// <param name="AggregateId">The identifier of the source feature toggle aggregate.</param>
/// <param name="AggregateType">The type name of the source aggregate.</param>
/// <param name="Version">The schema version of this event.</param>
/// <param name="FeatureName">The name of the feature toggle that changed.</param>
/// <param name="IsEnabled">Whether the feature is now globally enabled.</param>
/// <param name="Scope">The evaluation scope of the feature toggle.</param>
/// <param name="CanaryPercentage">The canary rollout percentage (0-100).</param>
public sealed record FeatureToggleChangedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string AggregateId,
    string AggregateType,
    int Version,
    string FeatureName,
    bool IsEnabled,
    string Scope,
    int CanaryPercentage) : IIntegrationEvent;
