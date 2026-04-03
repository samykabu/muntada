namespace Muntada.Identity.Api.Dtos;

/// <summary>
/// Response DTO returned after a PAT is created. Contains the plaintext token
/// which is returned only once and cannot be retrieved later.
/// </summary>
/// <param name="PatId">The unique identifier of the created PAT.</param>
/// <param name="PlaintextToken">The plaintext token value (store securely; cannot be retrieved again).</param>
public sealed record PatCreatedResponse(Guid PatId, string PlaintextToken);
