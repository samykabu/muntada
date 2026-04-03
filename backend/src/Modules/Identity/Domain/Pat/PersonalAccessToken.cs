using Muntada.SharedKernel.Domain;

namespace Muntada.Identity.Domain.Pat;

/// <summary>
/// Aggregate root for Personal Access Tokens. Token stored as bcrypt hash.
/// Scoped to user + tenant. Uses opaque ID prefix <c>pat_</c>.
/// </summary>
public sealed class PersonalAccessToken : AggregateRoot<Guid>
{
    /// <summary>Gets the user who owns this token.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Gets the tenant this token is scoped to.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Gets the human-readable name for this token.</summary>
    public string Name { get; private set; } = null!;

    /// <summary>Gets the bcrypt hash of the token value.</summary>
    public string TokenHash { get; private set; } = null!;

    /// <summary>Gets the list of permission scopes granted to this token.</summary>
    public List<string> Scopes { get; private set; } = new();

    /// <summary>Gets the current status of this token.</summary>
    public PatStatus Status { get; private set; }

    /// <summary>Gets the UTC timestamp when this token expires.</summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>Gets the UTC timestamp of the last API call made with this token. Null if never used.</summary>
    public DateTimeOffset? LastUsedAt { get; private set; }

    private PersonalAccessToken() { } // EF Core

    /// <summary>
    /// Creates a new active personal access token.
    /// </summary>
    /// <param name="userId">The user who owns this token.</param>
    /// <param name="tenantId">The tenant this token is scoped to.</param>
    /// <param name="name">A human-readable name for the token.</param>
    /// <param name="tokenHash">The bcrypt hash of the generated token value.</param>
    /// <param name="scopes">The permission scopes granted to this token.</param>
    /// <param name="expiry">The duration before this token expires.</param>
    /// <returns>A new <see cref="PersonalAccessToken"/> in <see cref="PatStatus.Active"/> status.</returns>
    public static PersonalAccessToken Create(
        Guid userId,
        Guid tenantId,
        string name,
        string tokenHash,
        List<string> scopes,
        TimeSpan expiry)
    {
        return new PersonalAccessToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            Name = name,
            TokenHash = tokenHash,
            Scopes = scopes ?? new List<string>(),
            Status = PatStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.Add(expiry),
            LastUsedAt = null,
        };
    }

    /// <summary>
    /// Determines whether this token is still valid for authentication.
    /// A token is valid when its status is <see cref="PatStatus.Active"/>
    /// and it has not expired.
    /// </summary>
    /// <returns><c>true</c> if the token can be used; otherwise, <c>false</c>.</returns>
    public bool IsValid()
    {
        return Status == PatStatus.Active
            && DateTimeOffset.UtcNow < ExpiresAt;
    }

    /// <summary>
    /// Checks whether this token has been granted a specific permission scope.
    /// </summary>
    /// <param name="scope">The scope identifier to check (e.g., "rooms:read").</param>
    /// <returns><c>true</c> if the token includes the specified scope; otherwise, <c>false</c>.</returns>
    public bool HasScope(string scope)
    {
        return Scopes.Contains(scope, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Records that this token was used for an API call.
    /// Updates the <see cref="LastUsedAt"/> timestamp.
    /// </summary>
    public void RecordUsage()
    {
        LastUsedAt = DateTimeOffset.UtcNow;
        IncrementVersion();
    }

    /// <summary>
    /// Revokes this token, preventing further use.
    /// Only transitions from <see cref="PatStatus.Active"/> status.
    /// </summary>
    public void Revoke()
    {
        if (Status != PatStatus.Active) return;

        Status = PatStatus.Revoked;
        IncrementVersion();
    }
}

/// <summary>Lifecycle status of a personal access token.</summary>
public enum PatStatus
{
    /// <summary>Token is active and can be used for authentication.</summary>
    Active = 0,

    /// <summary>Token was explicitly revoked.</summary>
    Revoked = 1
}
