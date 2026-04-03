using Muntada.SharedKernel.Domain;

namespace Muntada.Identity.Domain.Session;

/// <summary>
/// Aggregate root representing an authenticated user session.
/// Tracks device info, refresh token binding, and supports revocation.
/// Uses opaque ID prefix <c>ses_</c>.
/// </summary>
public sealed class Session : AggregateRoot<Guid>
{
    /// <summary>Gets the user who owns this session.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Gets the tenant context for this session (nullable until tenant is selected).</summary>
    public Guid? TenantId { get; private set; }

    /// <summary>Gets the session lifecycle status.</summary>
    public SessionStatus Status { get; private set; }

    /// <summary>Gets the device information captured at session creation.</summary>
    public DeviceInfo DeviceInfo { get; private set; } = null!;

    /// <summary>Gets the ID of the bound refresh token.</summary>
    public Guid RefreshTokenId { get; private set; }

    /// <summary>Gets the last activity timestamp.</summary>
    public DateTimeOffset? LastActivityAt { get; private set; }

    /// <summary>Gets the session expiration timestamp.</summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    private Session() { } // EF Core

    /// <summary>
    /// Creates a new active session.
    /// </summary>
    public static Session Create(Guid userId, DeviceInfo deviceInfo, Guid refreshTokenId, TimeSpan lifetime)
    {
        return new Session
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = SessionStatus.Active,
            DeviceInfo = deviceInfo,
            RefreshTokenId = refreshTokenId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            LastActivityAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.Add(lifetime),
        };
    }

    /// <summary>Revokes this session immediately.</summary>
    public void Revoke()
    {
        if (Status != SessionStatus.Active) return;
        Status = SessionStatus.Revoked;
        IncrementVersion();
    }

    /// <summary>Marks this session as expired.</summary>
    public void Expire()
    {
        if (Status != SessionStatus.Active) return;
        Status = SessionStatus.Expired;
        IncrementVersion();
    }

    /// <summary>Records activity on this session.</summary>
    public void RecordActivity()
    {
        LastActivityAt = DateTimeOffset.UtcNow;
        IncrementVersion();
    }

    /// <summary>Checks if the session is currently valid.</summary>
    public bool IsValid() => Status == SessionStatus.Active && DateTimeOffset.UtcNow < ExpiresAt;
}

/// <summary>Session lifecycle status.</summary>
public enum SessionStatus
{
    /// <summary>Session is active and can be used.</summary>
    Active = 0,
    /// <summary>Session was explicitly revoked by user or system.</summary>
    Revoked = 1,
    /// <summary>Session expired due to timeout or inactivity.</summary>
    Expired = 2
}
