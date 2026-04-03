using Muntada.SharedKernel.Domain;

namespace Muntada.Identity.Domain.PasswordReset;

/// <summary>
/// Entity for password reset tokens. Token stored as SHA-256 hash.
/// One-time use, 1-hour expiry. Uses opaque ID prefix <c>prt_</c>.
/// </summary>
public sealed class PasswordResetToken : Entity<Guid>
{
    /// <summary>Gets the user who requested the password reset.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Gets the SHA-256 hash of the reset token.</summary>
    public string TokenHash { get; private set; } = null!;

    /// <summary>Gets the current status of this reset token.</summary>
    public PasswordResetTokenStatus Status { get; private set; }

    /// <summary>Gets the UTC timestamp when this token was created.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Gets the UTC timestamp when this token expires.</summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    private PasswordResetToken() { } // EF Core

    /// <summary>
    /// Creates a new pending password reset token.
    /// </summary>
    /// <param name="userId">The user requesting the password reset.</param>
    /// <param name="tokenHash">The SHA-256 hash of the generated token.</param>
    /// <param name="expiry">The duration before this token expires.</param>
    /// <returns>A new <see cref="PasswordResetToken"/> in <see cref="PasswordResetTokenStatus.Pending"/> status.</returns>
    public static PasswordResetToken Create(Guid userId, string tokenHash, TimeSpan expiry)
    {
        return new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            Status = PasswordResetTokenStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.Add(expiry),
        };
    }

    /// <summary>
    /// Determines whether this token is still valid for use.
    /// A token is valid when its status is <see cref="PasswordResetTokenStatus.Pending"/>
    /// and it has not expired.
    /// </summary>
    /// <returns><c>true</c> if the token can be used to reset a password; otherwise, <c>false</c>.</returns>
    public bool IsValid()
    {
        return Status == PasswordResetTokenStatus.Pending
            && DateTimeOffset.UtcNow < ExpiresAt;
    }

    /// <summary>
    /// Marks this token as used, preventing further use.
    /// Only transitions from <see cref="PasswordResetTokenStatus.Pending"/> status.
    /// </summary>
    public void MarkUsed()
    {
        if (Status != PasswordResetTokenStatus.Pending) return;

        Status = PasswordResetTokenStatus.Used;
    }
}

/// <summary>Lifecycle status of a password reset token.</summary>
public enum PasswordResetTokenStatus
{
    /// <summary>Token is pending and can be used.</summary>
    Pending = 0,

    /// <summary>Token has been used to reset a password.</summary>
    Used = 1,

    /// <summary>Token expired without being used.</summary>
    Expired = 2
}
