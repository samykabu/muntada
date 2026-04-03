using Muntada.SharedKernel.Domain;

namespace Muntada.Identity.Domain.Otp;

/// <summary>
/// Aggregate root for phone OTP challenge. Tracks code, attempts, and expiry.
/// Uses opaque ID prefix <c>otp_</c>. Max 3 failed attempts before lockout.
/// </summary>
public sealed class OtpChallenge : AggregateRoot<Guid>
{
    /// <summary>Maximum number of failed verification attempts before lockout.</summary>
    private const int MaxFailedAttempts = 3;

    /// <summary>Gets the phone number in E.164 format that this challenge was sent to.</summary>
    public string PhoneNumber { get; private set; } = null!;

    /// <summary>Gets the SHA-256 hash of the 6-digit OTP code.</summary>
    public string CodeHash { get; private set; } = null!;

    /// <summary>Gets the current status of this OTP challenge.</summary>
    public OtpStatus Status { get; private set; }

    /// <summary>Gets the number of failed verification attempts.</summary>
    public int FailedAttempts { get; private set; }

    /// <summary>Gets the UTC timestamp when this challenge expires.</summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    private OtpChallenge() { } // EF Core

    /// <summary>
    /// Creates a new pending OTP challenge for the given phone number.
    /// </summary>
    /// <param name="phoneNumber">The phone number in E.164 format.</param>
    /// <param name="codeHash">The SHA-256 hash of the generated 6-digit code.</param>
    /// <param name="expiry">The duration before this challenge expires.</param>
    /// <returns>A new <see cref="OtpChallenge"/> in <see cref="OtpStatus.Pending"/> status.</returns>
    public static OtpChallenge Create(string phoneNumber, string codeHash, TimeSpan expiry)
    {
        return new OtpChallenge
        {
            Id = Guid.NewGuid(),
            PhoneNumber = phoneNumber,
            CodeHash = codeHash,
            Status = OtpStatus.Pending,
            FailedAttempts = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.Add(expiry),
        };
    }

    /// <summary>
    /// Determines whether this challenge is still valid for verification.
    /// A challenge is valid when its status is <see cref="OtpStatus.Pending"/>,
    /// it has not expired, and failed attempts are below the maximum threshold.
    /// </summary>
    /// <returns><c>true</c> if the challenge can still accept verification attempts; otherwise, <c>false</c>.</returns>
    public bool IsValid()
    {
        return Status == OtpStatus.Pending
            && DateTimeOffset.UtcNow < ExpiresAt
            && FailedAttempts < MaxFailedAttempts;
    }

    /// <summary>
    /// Records a failed verification attempt. If the maximum number of attempts
    /// is reached, the challenge status transitions to <see cref="OtpStatus.Expired"/>.
    /// </summary>
    public void IncrementFailedAttempts()
    {
        if (Status != OtpStatus.Pending) return;

        FailedAttempts++;

        if (FailedAttempts >= MaxFailedAttempts)
        {
            Status = OtpStatus.Expired;
        }

        IncrementVersion();
    }

    /// <summary>
    /// Marks this challenge as expired (e.g., by a cleanup job when past expiry time).
    /// Only transitions from <see cref="OtpStatus.Pending"/> status.
    /// </summary>
    public void MarkExpired()
    {
        if (Status != OtpStatus.Pending) return;

        Status = OtpStatus.Expired;
        IncrementVersion();
    }

    /// <summary>
    /// Marks this challenge as successfully verified.
    /// Only transitions from <see cref="OtpStatus.Pending"/> status.
    /// </summary>
    public void MarkVerified()
    {
        if (Status != OtpStatus.Pending) return;

        Status = OtpStatus.Verified;
        IncrementVersion();
    }
}

/// <summary>Lifecycle status of an OTP challenge.</summary>
public enum OtpStatus
{
    /// <summary>Challenge is awaiting verification.</summary>
    Pending = 0,

    /// <summary>Challenge was successfully verified.</summary>
    Verified = 1,

    /// <summary>Challenge expired or was locked out due to too many failed attempts.</summary>
    Expired = 2
}
