using System.Text.RegularExpressions;
using Muntada.SharedKernel.Domain;

namespace Muntada.Identity.Domain.User;

/// <summary>
/// Value object representing an E.164 formatted phone number.
/// Format: +{country-code}{subscriber-number} (e.g., +966501234567).
/// </summary>
public sealed partial class PhoneNumber : ValueObject
{
    /// <summary>
    /// Gets the E.164 formatted phone number string.
    /// </summary>
    public string Value { get; }

    private PhoneNumber(string value) => Value = value;

    /// <summary>
    /// Creates a validated <see cref="PhoneNumber"/> from a raw string.
    /// </summary>
    /// <param name="phoneNumber">The phone number in E.164 format.</param>
    /// <returns>A validated <see cref="PhoneNumber"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when the phone number is not valid E.164.</exception>
    public static PhoneNumber Create(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Phone number must not be empty.", nameof(phoneNumber));

        var normalized = phoneNumber.Trim();

        if (!E164Regex().IsMatch(normalized))
            throw new ArgumentException("Phone number must be in E.164 format (e.g., +966501234567).", nameof(phoneNumber));

        return new PhoneNumber(normalized);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc />
    public override string ToString() => Value;

    [GeneratedRegex(@"^\+[1-9]\d{6,14}$")]
    private static partial Regex E164Regex();
}
