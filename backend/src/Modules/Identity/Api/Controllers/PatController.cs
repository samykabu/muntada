using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Muntada.Identity.Api.Dtos;
using Muntada.Identity.Application.Commands;
using Muntada.Identity.Application.Queries;

namespace Muntada.Identity.Api.Controllers;

/// <summary>
/// Handles Personal Access Token operations: creating, listing, and revoking PATs.
/// </summary>
[ApiController]
[Route("api/v1/identity/pats")]
public sealed class PatController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes a new instance of <see cref="PatController"/>.
    /// </summary>
    /// <param name="sender">MediatR sender for dispatching commands and queries.</param>
    public PatController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Creates a new Personal Access Token. The plaintext token is returned only once
    /// and cannot be retrieved later.
    /// </summary>
    /// <param name="request">The request containing the PAT name, scopes, and expiry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>201 Created with the PAT details including the plaintext token.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(PatCreatedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePat(
        [FromBody] CreatePatRequest request,
        CancellationToken cancellationToken)
    {
        // TODO: Extract UserId and TenantId from authenticated user claims
        var userId = Guid.Empty;
        var tenantId = Guid.Empty;

        var command = new CreatePatCommand(userId, tenantId, request.Name, request.Scopes, request.ExpiresInDays);
        var result = await _sender.Send(command, cancellationToken);

        var response = new PatCreatedResponse(result.PatId, result.PlaintextToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>
    /// Lists all Personal Access Tokens for the authenticated user in the current tenant.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with a list of PAT DTOs (excluding token hashes).</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<PatDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListPats(CancellationToken cancellationToken)
    {
        // TODO: Extract UserId and TenantId from authenticated user claims
        var userId = Guid.Empty;
        var tenantId = Guid.Empty;

        var query = new ListPatsQuery(userId, tenantId);
        var pats = await _sender.Send(query, cancellationToken);

        var response = pats.Select(p => new PatDto(
            p.PatId,
            p.Name,
            p.Scopes,
            p.Status,
            p.CreatedAt,
            p.ExpiresAt,
            p.LastUsedAt)).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Revokes a Personal Access Token by ID.
    /// </summary>
    /// <param name="id">The unique identifier of the PAT to revoke.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK if revoked; 404 Not Found if the PAT does not exist or is not owned.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokePat(
        Guid id,
        CancellationToken cancellationToken)
    {
        // TODO: Extract UserId from authenticated user claims
        var userId = Guid.Empty;

        var command = new RevokePatCommand(id, userId);
        var result = await _sender.Send(command, cancellationToken);

        if (!result)
            return NotFound();

        return Ok(result);
    }
}
