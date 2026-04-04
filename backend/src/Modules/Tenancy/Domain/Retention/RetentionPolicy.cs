using Muntada.SharedKernel.Domain;
using Muntada.SharedKernel.Domain.Exceptions;

namespace Muntada.Tenancy.Domain.Retention;

/// <summary>
/// Represents a tenant's data retention policy, defining how long different types
/// of data are preserved before automatic purging.
/// </summary>
public class RetentionPolicy : Entity<Guid>
{
    /// <summary>
    /// The minimum allowed value for audit log retention, in days (approximately 7 years).
    /// </summary>
    public const int MinAuditLogRetentionDays = 2555;

    /// <summary>
    /// The minimum allowed retention period for any data type, in days.
    /// </summary>
    public const int MinRetentionDays = 1;

    /// <summary>
    /// The maximum allowed retention period for any data type, in days (approximately 10 years).
    /// </summary>
    public const int MaxRetentionDays = 3650;

    /// <summary>
    /// Gets the identifier of the tenant this policy belongs to.
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Gets the number of days room recordings are retained. Default is 90 days.
    /// </summary>
    public int RecordingRetentionDays { get; private set; }

    /// <summary>
    /// Gets the number of days chat messages are retained. Default is 365 days.
    /// </summary>
    public int ChatMessageRetentionDays { get; private set; }

    /// <summary>
    /// Gets the number of days uploaded files are retained. Default is 365 days.
    /// </summary>
    public int FileRetentionDays { get; private set; }

    /// <summary>
    /// Gets the number of days audit logs are retained. Minimum is 2555 days (approximately 7 years). Default is 2555 days.
    /// </summary>
    public int AuditLogRetentionDays { get; private set; }

    /// <summary>
    /// Gets the number of days user activity logs are retained. Default is 365 days.
    /// </summary>
    public int UserActivityLogRetentionDays { get; private set; }

    /// <summary>
    /// Gets the UTC date and time when this policy was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Private constructor for EF Core materialization.
    /// </summary>
    private RetentionPolicy() { }

    /// <summary>
    /// Creates a default retention policy for a tenant with standard retention periods.
    /// </summary>
    /// <param name="tenantId">The identifier of the tenant.</param>
    /// <returns>A new <see cref="RetentionPolicy"/> with default values.</returns>
    public static RetentionPolicy CreateDefault(Guid tenantId)
    {
        return new RetentionPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RecordingRetentionDays = 90,
            ChatMessageRetentionDays = 365,
            FileRetentionDays = 365,
            AuditLogRetentionDays = MinAuditLogRetentionDays,
            UserActivityLogRetentionDays = 365,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Updates the retention policy with the specified values. Only non-null parameters are applied.
    /// All values must be between <see cref="MinRetentionDays"/> and <see cref="MaxRetentionDays"/>.
    /// Audit log retention must be at least <see cref="MinAuditLogRetentionDays"/> days.
    /// </summary>
    /// <param name="recording">New recording retention in days, or <c>null</c> to keep current value.</param>
    /// <param name="chat">New chat message retention in days, or <c>null</c> to keep current value.</param>
    /// <param name="file">New file retention in days, or <c>null</c> to keep current value.</param>
    /// <param name="auditLog">New audit log retention in days, or <c>null</c> to keep current value.</param>
    /// <param name="activity">New user activity log retention in days, or <c>null</c> to keep current value.</param>
    /// <exception cref="ValidationException">Thrown when any provided value violates retention constraints.</exception>
    public void Update(int? recording, int? chat, int? file, int? auditLog, int? activity)
    {
        if (recording.HasValue)
        {
            ValidateRange(recording.Value, nameof(RecordingRetentionDays));
            RecordingRetentionDays = recording.Value;
        }

        if (chat.HasValue)
        {
            ValidateRange(chat.Value, nameof(ChatMessageRetentionDays));
            ChatMessageRetentionDays = chat.Value;
        }

        if (file.HasValue)
        {
            ValidateRange(file.Value, nameof(FileRetentionDays));
            FileRetentionDays = file.Value;
        }

        if (auditLog.HasValue)
        {
            ValidateRange(auditLog.Value, nameof(AuditLogRetentionDays));
            if (auditLog.Value < MinAuditLogRetentionDays)
                throw new ValidationException(nameof(AuditLogRetentionDays),
                    $"Audit log retention must be at least {MinAuditLogRetentionDays} days (approximately 7 years).");
            AuditLogRetentionDays = auditLog.Value;
        }

        if (activity.HasValue)
        {
            ValidateRange(activity.Value, nameof(UserActivityLogRetentionDays));
            UserActivityLogRetentionDays = activity.Value;
        }

        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateRange(int value, string propertyName)
    {
        if (value < MinRetentionDays || value > MaxRetentionDays)
            throw new ValidationException(propertyName,
                $"Retention period must be between {MinRetentionDays} and {MaxRetentionDays} days.");
    }
}
