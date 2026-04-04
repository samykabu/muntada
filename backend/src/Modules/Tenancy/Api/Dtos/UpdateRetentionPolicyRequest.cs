namespace Muntada.Tenancy.Api.Dtos;

/// <summary>
/// Request DTO for updating a tenant's data retention policy.
/// Only non-null values are applied; existing values are preserved when <c>null</c>.
/// </summary>
/// <param name="RecordingDays">New recording retention in days (1-3650), or <c>null</c> to keep current.</param>
/// <param name="ChatDays">New chat message retention in days (1-3650), or <c>null</c> to keep current.</param>
/// <param name="FileDays">New file retention in days (1-3650), or <c>null</c> to keep current.</param>
/// <param name="AuditLogDays">New audit log retention in days (2555-3650), or <c>null</c> to keep current.</param>
/// <param name="ActivityDays">New activity log retention in days (1-3650), or <c>null</c> to keep current.</param>
public sealed record UpdateRetentionPolicyRequest(
    int? RecordingDays,
    int? ChatDays,
    int? FileDays,
    int? AuditLogDays,
    int? ActivityDays);
