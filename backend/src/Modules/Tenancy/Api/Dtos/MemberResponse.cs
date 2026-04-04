namespace Muntada.Tenancy.Api.Dtos;

/// <summary>
/// Response DTO representing a tenant member's details.
/// </summary>
/// <param name="Id">The unique identifier of the membership.</param>
/// <param name="UserId">The identifier of the user, or <c>null</c> if the invite is pending.</param>
/// <param name="Email">The email address of the member or invited user.</param>
/// <param name="DisplayName">The display name of the member, or <c>null</c> if not yet known.</param>
/// <param name="Role">The role assigned to the member within the tenant.</param>
/// <param name="Status">The current membership status (Active, Pending, Inactive).</param>
/// <param name="JoinedAt">The UTC date and time when the member joined, or <c>null</c> if pending.</param>
/// <param name="LastActivityAt">The UTC date and time of last activity, or <c>null</c>.</param>
public sealed record MemberResponse(
    Guid Id,
    Guid? UserId,
    string? Email,
    string? DisplayName,
    string Role,
    string Status,
    DateTime? JoinedAt,
    DateTime? LastActivityAt);
