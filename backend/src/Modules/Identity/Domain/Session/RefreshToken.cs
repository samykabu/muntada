using Muntada.SharedKernel.Domain;

namespace Muntada.Identity.Domain.Session;

/// <summary>
/// Entity representing an opaque refresh token bound to a session.
/// Token is stored as a bcrypt hash — plaintext is never persisted.
/// </summary>
public sealed class RefreshToken : Entity<Guid>
{
    /// <summary>Gets the session this token is bound to.</summary>
    public Guid SessionId { get; private set; }

    /// <summary>Gets the bcrypt hash of the token value.</summary>
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>Gets the token status.</summary>
    public RefreshTokenStatus Status { get; private set; }

    /// <summary>Gets the creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Gets the expiration timestamp.</summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>Gets the revocation timestamp (null if not revoked).</summary>
    public DateTimeOffset? RevokedAt { get; private set; }

    private RefreshToken() { } // EF Core

    /// <summary>Creates a new active refresh token.</summary>
    public static RefreshToken Create(Guid sessionId, string tokenHash, TimeSpan lifetime)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            TokenHash = tokenHash,
            Status = RefreshTokenStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.Add(lifetime),
        };
    }

    /// <summary>Checks if the token is valid (active and not expired).</summary>
    public bool IsValid() => Status == RefreshTokenStatus.Active && DateTimeOffset.UtcNow < ExpiresAt;

    /// <summary>Revokes this refresh token.</summary>
    public void Revoke()
    {
        Status = RefreshTokenStatus.Revoked;
        RevokedAt = DateTimeOffset.UtcNow;
    }
}

/// <summary>Refresh token lifecycle status.</summary>
public enum RefreshTokenStatus
{
    /// <summary>Token is active and can be used.</summary>
    Active = 0,
    /// <summary>Token was explicitly revoked.</summary>
    Revoked = 1,
    /// <summary>Token has expired.</summary>
    Expired = 2
}
