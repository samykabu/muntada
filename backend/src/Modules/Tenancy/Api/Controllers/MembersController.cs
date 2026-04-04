using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Muntada.Tenancy.Api.Dtos;
using Muntada.Tenancy.Application.Commands;
using Muntada.Tenancy.Application.Queries;
using Muntada.Tenancy.Domain.Membership;

namespace Muntada.Tenancy.Api.Controllers;

/// <summary>
/// Handles tenant membership operations: listing, inviting, accepting invites,
/// updating roles, and removing members.
/// </summary>
[ApiController]
[Route("api/v1/tenants/{tenantId:guid}/members")]
public sealed class MembersController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes a new instance of <see cref="MembersController"/>.
    /// </summary>
    /// <param name="sender">MediatR sender for dispatching commands and queries.</param>
    public MembersController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Retrieves a paginated list of members for a tenant, optionally filtered by status.
    /// </summary>
    /// <param name="tenantId">The identifier of the tenant.</param>
    /// <param name="status">Optional membership status filter (Active, Pending, Inactive).</param>
    /// <param name="page">The 1-based page number (defaults to 1).</param>
    /// <param name="pageSize">The number of items per page (defaults to 20, max 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the paginated list of members.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedMembersResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMembers(
        Guid tenantId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        TenantMembershipStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<TenantMembershipStatus>(status, ignoreCase: true, out var parsedStatus))
        {
            statusFilter = parsedStatus;
        }

        var query = new GetTenantMembersQuery(tenantId, statusFilter, page, pageSize);
        var result = await _sender.Send(query, cancellationToken);

        var items = result.Items.Select(m => new MemberResponse(
            Id: m.Id,
            UserId: m.UserId,
            Email: m.Email,
            DisplayName: m.DisplayName,
            Role: m.Role,
            Status: m.Status,
            JoinedAt: m.JoinedAt,
            LastActivityAt: m.LastActivityAt)).ToList();

        return Ok(new PaginatedMembersResponse(items, result.TotalCount, result.Page, result.PageSize));
    }

    /// <summary>
    /// Invites a new member to the tenant via email.
    /// The inviter must be an Owner or Admin. Admins cannot invite with the Owner role.
    /// </summary>
    /// <param name="tenantId">The identifier of the tenant.</param>
    /// <param name="request">The invitation details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>201 Created with the invitation details and token expiry.</returns>
    [HttpPost("invite")]
    [ProducesResponseType(typeof(InviteMemberResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> InviteMember(
        Guid tenantId,
        [FromBody] InviteMemberRequest request,
        CancellationToken cancellationToken)
    {
        var invitedBy = GetAuthenticatedUserId();

        if (!Enum.TryParse<TenantRole>(request.Role, ignoreCase: true, out var role))
        {
            return BadRequest(new { Error = $"Invalid role '{request.Role}'. Valid roles are: Owner, Admin, Member." });
        }

        var command = new InviteTenantMemberCommand(
            tenantId,
            request.Email,
            role,
            invitedBy,
            request.Message);

        var result = await _sender.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetMembers),
            new { tenantId },
            result);
    }

    /// <summary>
    /// Accepts a pending tenant membership invitation using an invite token.
    /// The user accepting the invite must be authenticated.
    /// </summary>
    /// <param name="tenantId">The identifier of the tenant (used for route context).</param>
    /// <param name="request">The acceptance request containing the invite token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the acceptance details.</returns>
    [HttpPost("accept")]
    [ProducesResponseType(typeof(AcceptInviteResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AcceptInvite(
        Guid tenantId,
        [FromBody] AcceptInviteRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();

        var command = new AcceptTenantInviteCommand(request.Token, userId);
        var result = await _sender.Send(command, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Updates the role of a tenant member. Only tenant Owners may change member roles.
    /// </summary>
    /// <param name="tenantId">The identifier of the tenant.</param>
    /// <param name="memberId">The identifier of the membership to update.</param>
    /// <param name="request">The role update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [HttpPatch("{memberId:guid}/role")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateMemberRole(
        Guid tenantId,
        Guid memberId,
        [FromBody] UpdateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var requestedBy = GetAuthenticatedUserId();

        if (!Enum.TryParse<TenantRole>(request.Role, ignoreCase: true, out var newRole))
        {
            return BadRequest(new { Error = $"Invalid role '{request.Role}'. Valid roles are: Owner, Admin, Member." });
        }

        var command = new UpdateTenantMemberRoleCommand(tenantId, memberId, newRole, requestedBy);
        await _sender.Send(command, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Removes (deactivates) a member from the tenant. Owners and Admins can remove members,
    /// but Admins cannot remove Owners. The last Owner cannot be removed.
    /// </summary>
    /// <param name="tenantId">The identifier of the tenant.</param>
    /// <param name="memberId">The identifier of the membership to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [HttpDelete("{memberId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoveMember(
        Guid tenantId,
        Guid memberId,
        CancellationToken cancellationToken)
    {
        var requestedBy = GetAuthenticatedUserId();

        var command = new RemoveTenantMemberCommand(tenantId, memberId, requestedBy);
        await _sender.Send(command, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Extracts the authenticated user's identifier from JWT claims.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">Thrown when no valid user identifier is found in claims.</exception>
    private Guid GetAuthenticatedUserId() =>
        Guid.TryParse(
            User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            out var id)
            ? id
            : throw new UnauthorizedAccessException("User not authenticated");
}

/// <summary>
/// Paginated response DTO for tenant members.
/// </summary>
/// <param name="Items">The list of member items for the current page.</param>
/// <param name="TotalCount">The total number of members matching the filter.</param>
/// <param name="Page">The current page number.</param>
/// <param name="PageSize">The number of items per page.</param>
public sealed record PaginatedMembersResponse(
    IReadOnlyList<MemberResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);
