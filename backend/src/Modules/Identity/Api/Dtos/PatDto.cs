namespace Muntada.Identity.Api.Dtos;

/// <summary>
/// Response DTO representing a Personal Access Token in list views (excludes the token hash).
/// </summary>
/// <param name="PatId">The unique identifier of the PAT.</param>
/// <param name="Name">The human-readable name of the PAT.</param>
/// <param name="Scopes">The permission scopes granted to this PAT.</param>
/// <param name="Status">The current status of the PAT (e.g., "Active", "Revoked").</param>
/// <param name="CreatedAt">The UTC timestamp when the PAT was created.</param>
/// <param name="ExpiresAt">The UTC timestamp when the PAT expires.</param>
/// <param name="LastUsedAt">The UTC timestamp of the last API call made with this PAT.</param>
public sealed record PatDto(
    Guid PatId,
    string Name,
    List<string> Scopes,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? LastUsedAt);
