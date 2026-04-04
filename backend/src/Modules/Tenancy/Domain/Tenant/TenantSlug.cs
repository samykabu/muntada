using System.Text.RegularExpressions;
using Muntada.SharedKernel.Domain;
using Muntada.SharedKernel.Domain.Exceptions;

namespace Muntada.Tenancy.Domain.Tenant;

/// <summary>
/// Value object representing a URL-safe tenant slug used in routing and subdomains.
/// Must be 3-63 lowercase alphanumeric characters or hyphens, cannot start or end
/// with a hyphen, and cannot be a reserved word.
/// </summary>
public sealed partial class TenantSlug : ValueObject
{
    private static readonly HashSet<string> ReservedWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin", "api", "www", "app", "help",
        "support", "billing", "status", "mail", "ftp"
    };

    /// <summary>
    /// Gets the string value of the slug.
    /// </summary>
    public string Value { get; }

    private TenantSlug(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new <see cref="TenantSlug"/> after validating format, length, and reserved-word constraints.
    /// </summary>
    /// <param name="value">The slug value to validate and wrap.</param>
    /// <returns>A valid <see cref="TenantSlug"/> instance.</returns>
    /// <exception cref="ValidationException">Thrown when the slug is invalid.</exception>
    public static TenantSlug Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ValidationException(nameof(TenantSlug), "Tenant slug cannot be empty.");

        if (value.Length < 3 || value.Length > 63)
            throw new ValidationException(nameof(TenantSlug), "Tenant slug must be between 3 and 63 characters.");

        if (!SlugRegex().IsMatch(value))
            throw new ValidationException(nameof(TenantSlug),
                "Tenant slug must contain only lowercase alphanumeric characters and hyphens, " +
                "and must start and end with an alphanumeric character.");

        if (ReservedWords.Contains(value))
            throw new ValidationException(nameof(TenantSlug), $"'{value}' is a reserved word and cannot be used as a tenant slug.");

        return new TenantSlug(value);
    }

    /// <summary>
    /// Generates a URL-safe slug from a display name by converting to lowercase,
    /// replacing non-alphanumeric characters with hyphens, collapsing consecutive
    /// hyphens, trimming leading/trailing hyphens, and truncating to 63 characters.
    /// </summary>
    /// <param name="name">The display name to convert.</param>
    /// <returns>A valid <see cref="TenantSlug"/> instance derived from the name.</returns>
    /// <exception cref="ValidationException">Thrown when the resulting slug is invalid.</exception>
    public static TenantSlug GenerateFromName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException(nameof(TenantSlug), "Name cannot be empty.");

        var slug = name.ToLowerInvariant();
        slug = NonAlphanumericRegex().Replace(slug, "-");
        slug = ConsecutiveHyphensRegex().Replace(slug, "-");
        slug = slug.Trim('-');

        if (slug.Length > 63)
            slug = slug[..63].TrimEnd('-');

        return Create(slug);
    }

    /// <summary>
    /// Implicitly converts a <see cref="TenantSlug"/> to its string representation.
    /// </summary>
    /// <param name="slug">The slug to convert.</param>
    public static implicit operator string(TenantSlug slug) => slug.Value;

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    [GeneratedRegex(@"^[a-z0-9][a-z0-9-]*[a-z0-9]$")]
    private static partial Regex SlugRegex();

    [GeneratedRegex(@"[^a-z0-9]")]
    private static partial Regex NonAlphanumericRegex();

    [GeneratedRegex(@"-{2,}")]
    private static partial Regex ConsecutiveHyphensRegex();
}
