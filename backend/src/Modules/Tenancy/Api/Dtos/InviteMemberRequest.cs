namespace Muntada.Tenancy.Api.Dtos;

/// <summary>
/// Request DTO for inviting a new member to a tenant.
/// </summary>
/// <param name="Email">The email address of the user to invite.</param>
/// <param name="Role">The role to assign upon acceptance (Admin or Member).</param>
/// <param name="Message">Optional personal message to include in the invitation email.</param>
public sealed record InviteMemberRequest(
    string Email,
    string Role,
    string? Message);
