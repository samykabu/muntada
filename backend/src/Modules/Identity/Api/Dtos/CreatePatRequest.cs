namespace Muntada.Identity.Api.Dtos;

/// <summary>
/// Request DTO for creating a Personal Access Token.
/// </summary>
/// <param name="Name">A human-readable name for the token.</param>
/// <param name="Scopes">The permission scopes to grant to this token.</param>
/// <param name="ExpiresInDays">The number of days until the token expires.</param>
public sealed record CreatePatRequest(string Name, List<string> Scopes, int ExpiresInDays);
