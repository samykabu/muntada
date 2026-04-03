using Sqids;

namespace Muntada.SharedKernel.Domain;

/// <summary>
/// Generates URL-safe opaque identifiers with a type prefix.
/// Format: <c>{prefix}_{encoded}</c> (e.g., <c>usr_a7k2jZ9xQpR4b1m</c>).
/// Uses Sqids for encoding and a cryptographic random source for uniqueness.
/// </summary>
/// <remarks>
/// Constitution VII (Explicit Over Implicit): Opaque identifiers prevent
/// enumeration attacks and hide internal implementation details.
/// </remarks>
public static class OpaqueIdGenerator
{
    private static readonly SqidsEncoder<int> Encoder = new(new SqidsOptions
    {
        MinLength = 10,
        Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"
    });

    /// <summary>
    /// Generates a new opaque identifier with the specified prefix.
    /// </summary>
    /// <param name="prefix">
    /// The type prefix (2-8 lowercase alphabetic characters).
    /// Examples: "usr", "org", "room", "msg".
    /// </param>
    /// <returns>A new opaque identifier in the format <c>{prefix}_{encoded}</c>.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="prefix"/> is null, empty, longer than 8 characters,
    /// or contains non-lowercase-alphabetic characters.
    /// </exception>
    public static string Generate(string prefix)
    {
        ValidatePrefix(prefix);

        var randomValue = GenerateRandomInt();
        var encoded = Encoder.Encode(randomValue);
        return $"{prefix}_{encoded}";
    }

    /// <summary>
    /// Attempts to parse an opaque identifier string into its prefix and encoded parts.
    /// </summary>
    /// <param name="input">The opaque identifier string to parse.</param>
    /// <param name="prefix">When successful, the extracted prefix.</param>
    /// <param name="encodedPart">When successful, the extracted encoded part.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
    public static bool TryParse(string input, out string prefix, out string encodedPart)
    {
        prefix = string.Empty;
        encodedPart = string.Empty;

        if (string.IsNullOrWhiteSpace(input))
            return false;

        var separatorIndex = input.IndexOf('_');
        if (separatorIndex < 2 || separatorIndex > 8)
            return false;

        var candidatePrefix = input[..separatorIndex];
        var candidateEncoded = input[(separatorIndex + 1)..];

        if (string.IsNullOrEmpty(candidateEncoded))
            return false;

        if (!candidatePrefix.All(c => c >= 'a' && c <= 'z'))
            return false;

        prefix = candidatePrefix;
        encodedPart = candidateEncoded;
        return true;
    }

    private static void ValidatePrefix(string prefix)
    {
        if (string.IsNullOrEmpty(prefix))
            throw new ArgumentException("Prefix must not be null or empty.", nameof(prefix));

        if (prefix.Length < 2 || prefix.Length > 8)
            throw new ArgumentException("Prefix must be 2-8 characters long.", nameof(prefix));

        if (!prefix.All(c => c >= 'a' && c <= 'z'))
            throw new ArgumentException("Prefix must contain only lowercase alphabetic characters.", nameof(prefix));
    }

    private static int GenerateRandomInt() =>
        System.Security.Cryptography.RandomNumberGenerator.GetInt32(0, int.MaxValue);
}
