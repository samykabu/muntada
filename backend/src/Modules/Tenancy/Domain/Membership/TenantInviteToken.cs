using System.Security.Cryptography;
using Muntada.SharedKernel.Domain;

namespace Muntada.Tenancy.Domain.Membership;

/// <summary>
/// Represents a time-limited token used to verify and complete a tenant membership invitation.
/// Tokens are single-use, base64url-encoded, and expire after 7 days.
/// </summary>
public class TenantInviteToken : Entity<Guid>
{
    /// <summary>
    /// Gets the identifier of the associated <see cref="TenantMembership"/>.
    /// </summary>
    public Guid MembershipId { get; private set; }

    /// <summary>
    /// Gets the base64url-encoded token string used for invite verification.
    /// </summary>
    public string Token { get; private set; } = default!;

    /// <summary>
    /// Gets the UTC date and time when this token expires.
    /// </summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this token has already been used.
    /// </summary>
    public bool IsUsed { get; private set; }

    /// <summary>
    /// Gets the UTC date and time when this token was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Private constructor for EF Core materialization.
    /// </summary>
    private TenantInviteToken() { }

    /// <summary>
    /// Generates a new invite token for the specified membership.
    /// Creates a cryptographically secure 32-byte random token encoded as base64url,
    /// with a 7-day expiration period.
    /// </summary>
    /// <param name="membershipId">The identifier of the membership this token is associated with.</param>
    /// <returns>A new <see cref="TenantInviteToken"/> instance.</returns>
    public static TenantInviteToken Generate(Guid membershipId)
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var tokenString = Convert.ToBase64String(tokenBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        return new TenantInviteToken
        {
            Id = Guid.NewGuid(),
            MembershipId = membershipId,
            Token = tokenString,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Marks this token as used, preventing it from being redeemed again.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the token has already been used.</exception>
    public void MarkAsUsed()
    {
        if (IsUsed)
            throw new InvalidOperationException("This invite token has already been used.");

        IsUsed = true;
    }

    /// <summary>
    /// Determines whether this token has expired based on the current UTC time.
    /// </summary>
    /// <returns><c>true</c> if the token has expired; otherwise, <c>false</c>.</returns>
    public bool IsExpired()
    {
        return ExpiresAt < DateTime.UtcNow;
    }
}
