namespace Muntada.SharedKernel.Domain;

/// <summary>
/// Records a change to an audited entity for compliance and traceability.
/// Stored per-module in the module's SQL Server schema.
/// </summary>
public class AuditLog
{
    /// <summary>
    /// Gets or sets the unique identifier for this audit log entry.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the full type name of the audited entity.
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the opaque ID of the audited entity.
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action performed on the entity.
    /// </summary>
    public AuditAction Action { get; set; }

    /// <summary>
    /// Gets or sets the opaque user ID who made the change.
    /// </summary>
    public string ChangedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp of the change.
    /// </summary>
    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the JSON-serialized before/after values.
    /// </summary>
    public string? Changes { get; set; }

    /// <summary>
    /// Gets or sets the request correlation ID for distributed tracing.
    /// </summary>
    public string? CorrelationId { get; set; }
}

/// <summary>
/// The type of action performed on an audited entity.
/// </summary>
public enum AuditAction
{
    /// <summary>Entity was created.</summary>
    Created = 0,

    /// <summary>Entity was updated.</summary>
    Updated = 1,

    /// <summary>Entity was deleted (soft or hard).</summary>
    Deleted = 2
}
