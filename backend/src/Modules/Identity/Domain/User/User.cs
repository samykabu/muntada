using Muntada.SharedKernel.Domain;

namespace Muntada.Identity.Domain.User;

/// <summary>
/// Aggregate root representing a platform user.
/// Owns sessions and PATs. Uses opaque ID prefix <c>usr_</c>.
/// </summary>
public sealed class User : AuditedEntity<Guid>
{
    /// <summary>
    /// Gets the user's validated email address.
    /// </summary>
    public Email Email { get; private set; } = null!;

    /// <summary>
    /// Gets the user's bcrypt-hashed password.
    /// </summary>
    public PasswordHash PasswordHash { get; private set; } = null!;

    /// <summary>
    /// Gets the user's phone number (optional, E.164 format).
    /// Required for phone OTP login.
    /// </summary>
    public PhoneNumber? PhoneNumber { get; private set; }

    /// <summary>
    /// Gets the user's account status.
    /// </summary>
    public UserStatus Status { get; private set; }

    /// <summary>
    /// Gets or sets the user's full display name.
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// Gets the timestamp of the user's last successful login.
    /// </summary>
    public DateTimeOffset? LastLoginAt { get; private set; }

    private User() { } // EF Core

    /// <summary>
    /// Creates a new user with Unverified status.
    /// </summary>
    public static User Create(Email email, PasswordHash passwordHash, string createdBy)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHash,
            Status = UserStatus.Unverified,
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        return user;
    }

    /// <summary>
    /// Activates the user after email verification.
    /// </summary>
    public void Activate()
    {
        if (Status != UserStatus.Unverified)
            throw new InvalidOperationException($"Cannot activate user with status {Status}.");
        Status = UserStatus.Active;
        IncrementVersion();
    }

    /// <summary>
    /// Suspends the user account (admin action).
    /// </summary>
    public void Suspend()
    {
        if (Status != UserStatus.Active)
            throw new InvalidOperationException($"Cannot suspend user with status {Status}.");
        Status = UserStatus.Suspended;
        IncrementVersion();
    }

    /// <summary>
    /// Records a successful login timestamp.
    /// </summary>
    public void RecordLogin()
    {
        LastLoginAt = DateTimeOffset.UtcNow;
        IncrementVersion();
    }

    /// <summary>
    /// Updates the password hash.
    /// </summary>
    public void ChangePassword(PasswordHash newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        IncrementVersion();
    }

    /// <summary>
    /// Sets the phone number for OTP login.
    /// </summary>
    public void SetPhoneNumber(PhoneNumber phoneNumber)
    {
        PhoneNumber = phoneNumber;
        IncrementVersion();
    }
}

/// <summary>
/// Represents the lifecycle status of a user account.
/// </summary>
public enum UserStatus
{
    /// <summary>Email not yet verified.</summary>
    Unverified = 0,
    /// <summary>Fully activated — can log in.</summary>
    Active = 1,
    /// <summary>Temporarily disabled by admin or security.</summary>
    Suspended = 2,
    /// <summary>Soft-deleted.</summary>
    Deleted = 3
}
