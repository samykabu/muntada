namespace Muntada.Tenancy.Api.Dtos;

/// <summary>
/// Response DTO representing a tenant's data retention policy configuration.
/// </summary>
/// <param name="TenantId">The identifier of the tenant.</param>
/// <param name="RecordingRetentionDays">Number of days room recordings are retained.</param>
/// <param name="ChatMessageRetentionDays">Number of days chat messages are retained.</param>
/// <param name="FileRetentionDays">Number of days uploaded files are retained.</param>
/// <param name="AuditLogRetentionDays">Number of days audit logs are retained.</param>
/// <param name="UserActivityLogRetentionDays">Number of days user activity logs are retained.</param>
/// <param name="UpdatedAt">UTC timestamp of the last policy update.</param>
/// <param name="AllowedRange">The allowed ranges for configuring retention values.</param>
public sealed record RetentionPolicyResponse(
    Guid TenantId,
    int RecordingRetentionDays,
    int ChatMessageRetentionDays,
    int FileRetentionDays,
    int AuditLogRetentionDays,
    int UserActivityLogRetentionDays,
    DateTime UpdatedAt,
    RetentionAllowedRangeResponse AllowedRange);

/// <summary>
/// Response DTO describing the allowed ranges for retention configuration values.
/// </summary>
/// <param name="MinDays">The minimum allowed retention period in days.</param>
/// <param name="MaxDays">The maximum allowed retention period in days.</param>
/// <param name="MinAuditLogDays">The minimum allowed audit log retention period in days.</param>
public sealed record RetentionAllowedRangeResponse(
    int MinDays,
    int MaxDays,
    int MinAuditLogDays);
