using Muntada.SharedKernel.Domain;

namespace Muntada.Identity.Domain.EmailVerification;

/// <summary>
/// Entity for email verification tokens. Token stored as SHA-256 hash.
/// One-time use, 24-hour expiry. Uses opaque ID prefix <c>evt_</c>.
/// </summary>
public sealed class EmailVerificationToken : Entity<Guid>
{
    /// <summary>Gets the user whose email address is being verified.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Gets the SHA-256 hash of the verification token.</summary>
    public string TokenHash { get; private set; } = null!;

    /// <summary>Gets the current status of this verification token.</summary>
    public EmailVerificationTokenStatus Status { get; private set; }

    /// <summary>Gets the UTC timestamp when this token was created.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Gets the UTC timestamp when this token expires.</summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    private EmailVerificationToken() { } // EF Core

    /// <summary>
    /// Creates a new pending email verification token.
    /// </summary>
    /// <param name="userId">The user whose email address is being verified.</param>
    /// <param name="tokenHash">The SHA-256 hash of the generated token.</param>
    /// <param name="expiry">The duration before this token expires.</param>
    /// <returns>A new <see cref="EmailVerificationToken"/> in <see cref="EmailVerificationTokenStatus.Pending"/> status.</returns>
    public static EmailVerificationToken Create(Guid userId, string tokenHash, TimeSpan expiry)
    {
        return new EmailVerificationToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            Status = EmailVerificationTokenStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.Add(expiry),
        };
    }

    /// <summary>
    /// Determines whether this token is still valid for use.
    /// A token is valid when its status is <see cref="EmailVerificationTokenStatus.Pending"/>
    /// and it has not expired.
    /// </summary>
    /// <returns><c>true</c> if the token can be used to verify an email; otherwise, <c>false</c>.</returns>
    public bool IsValid()
    {
        return Status == EmailVerificationTokenStatus.Pending
            && DateTimeOffset.UtcNow < ExpiresAt;
    }

    /// <summary>
    /// Marks this token as used, preventing further use.
    /// Only transitions from <see cref="EmailVerificationTokenStatus.Pending"/> status.
    /// </summary>
    public void MarkUsed()
    {
        if (Status != EmailVerificationTokenStatus.Pending) return;

        Status = EmailVerificationTokenStatus.Used;
    }
}

/// <summary>Lifecycle status of an email verification token.</summary>
public enum EmailVerificationTokenStatus
{
    /// <summary>Token is pending and can be used.</summary>
    Pending = 0,

    /// <summary>Token has been used to verify an email address.</summary>
    Used = 1,

    /// <summary>Token expired without being used.</summary>
    Expired = 2
}
