namespace Muntada.Tenancy.Api.Dtos;

/// <summary>
/// Request DTO for accepting a tenant membership invitation.
/// </summary>
/// <param name="Token">The invite token received via email.</param>
public sealed record AcceptInviteRequest(string Token);
