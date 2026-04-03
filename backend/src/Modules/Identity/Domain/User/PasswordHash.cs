using Muntada.SharedKernel.Domain;

namespace Muntada.Identity.Domain.User;

/// <summary>
/// Value object representing a bcrypt-hashed password.
/// Passwords are hashed with cost factor 12 (FR-003).
/// Plaintext passwords are never stored or logged.
/// </summary>
public sealed class PasswordHash : ValueObject
{
    private const int BcryptCostFactor = 12;

    /// <summary>
    /// Gets the bcrypt hash string.
    /// </summary>
    public string Hash { get; }

    private PasswordHash(string hash) => Hash = hash;

    /// <summary>
    /// Creates a <see cref="PasswordHash"/> by hashing a plaintext password.
    /// </summary>
    /// <param name="plaintext">The plaintext password to hash.</param>
    /// <returns>A new <see cref="PasswordHash"/> containing the bcrypt hash.</returns>
    /// <exception cref="ArgumentException">Thrown when the password does not meet complexity requirements.</exception>
    public static PasswordHash Create(string plaintext)
    {
        ValidateComplexity(plaintext);
        var hash = BCrypt.Net.BCrypt.HashPassword(plaintext, BcryptCostFactor);
        return new PasswordHash(hash);
    }

    /// <summary>
    /// Creates a <see cref="PasswordHash"/> from an existing hash (e.g., from database).
    /// </summary>
    /// <param name="existingHash">The existing bcrypt hash.</param>
    /// <returns>A <see cref="PasswordHash"/> wrapping the existing hash.</returns>
    public static PasswordHash FromHash(string existingHash) => new(existingHash);

    /// <summary>
    /// Verifies a plaintext password against this hash.
    /// </summary>
    /// <param name="plaintext">The plaintext password to verify.</param>
    /// <returns><c>true</c> if the password matches; otherwise, <c>false</c>.</returns>
    public bool Verify(string plaintext) => BCrypt.Net.BCrypt.Verify(plaintext, Hash);

    /// <summary>
    /// Validates password complexity: minimum 12 chars, 1 uppercase, 1 digit, 1 special character.
    /// </summary>
    public static void ValidateComplexity(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 12)
            throw new ArgumentException("Password must be at least 12 characters.", nameof(password));
        if (!password.Any(char.IsUpper))
            throw new ArgumentException("Password must contain at least 1 uppercase letter.", nameof(password));
        if (!password.Any(char.IsDigit))
            throw new ArgumentException("Password must contain at least 1 digit.", nameof(password));
        if (password.All(char.IsLetterOrDigit))
            throw new ArgumentException("Password must contain at least 1 special character.", nameof(password));
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Hash;
    }
}
