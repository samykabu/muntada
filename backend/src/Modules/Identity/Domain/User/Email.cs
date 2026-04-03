using Muntada.SharedKernel.Domain;

namespace Muntada.Identity.Domain.User;

/// <summary>
/// Value object representing a validated, normalized email address.
/// Emails are stored lowercase and validated per RFC 5322 basics.
/// </summary>
public sealed class Email : ValueObject
{
    /// <summary>
    /// Gets the normalized (lowercase, trimmed) email address.
    /// </summary>
    public string Value { get; }

    private Email(string value) => Value = value;

    /// <summary>
    /// Creates a new <see cref="Email"/> from a raw string.
    /// </summary>
    /// <param name="email">The raw email address.</param>
    /// <returns>A validated, normalized <see cref="Email"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when the email is invalid.</exception>
    public static Email Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email must not be empty.", nameof(email));

        var normalized = email.Trim().ToLowerInvariant();

        if (!IsValidFormat(normalized))
            throw new ArgumentException("Email format is invalid.", nameof(email));

        return new Email(normalized);
    }

    /// <summary>
    /// Basic email format validation.
    /// </summary>
    public static bool IsValidFormat(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        var atIndex = email.IndexOf('@');
        if (atIndex < 1) return false;
        var dotIndex = email.LastIndexOf('.');
        return dotIndex > atIndex + 1 && dotIndex < email.Length - 1;
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
