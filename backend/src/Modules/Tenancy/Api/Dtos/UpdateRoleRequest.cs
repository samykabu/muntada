namespace Muntada.Tenancy.Api.Dtos;

/// <summary>
/// Request DTO for updating a tenant member's role.
/// </summary>
/// <param name="Role">The new role to assign (Owner, Admin, or Member).</param>
public sealed record UpdateRoleRequest(string Role);
