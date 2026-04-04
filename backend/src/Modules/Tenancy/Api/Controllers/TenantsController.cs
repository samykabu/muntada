using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Muntada.Tenancy.Api.Dtos;
using Muntada.Tenancy.Application.Commands;
using Muntada.Tenancy.Application.Queries;

#pragma warning disable CS1573 // Not all parameters have XML doc tags (IFormFile params)

namespace Muntada.Tenancy.Api.Controllers;

/// <summary>
/// Handles tenant lifecycle operations: creation and retrieval.
/// </summary>
[ApiController]
[Route("api/v1/tenants")]
public sealed class TenantsController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes a new instance of <see cref="TenantsController"/>.
    /// </summary>
    /// <param name="sender">MediatR sender for dispatching commands and queries.</param>
    public TenantsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Creates a new tenant (organization/workspace) with a 14-day trial plan.
    /// The creating user is automatically assigned as the tenant owner.
    /// </summary>
    /// <param name="request">The tenant creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>201 Created with the new tenant's details.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTenant(
        [FromBody] CreateTenantRequest request,
        CancellationToken cancellationToken)
    {
        var createdBy = GetAuthenticatedUserId();

        var command = new CreateTenantCommand(
            request.Name,
            request.Slug,
            request.Industry,
            request.TeamSize,
            createdBy);

        var result = await _sender.Send(command, cancellationToken);

        var response = new TenantResponse(
            Id: result.TenantId,
            Name: result.Name,
            Slug: result.Slug,
            Status: result.Status,
            BillingStatus: result.BillingStatus,
            TrialEndsAt: result.TrialEndsAt,
            Branding: new TenantBrandingResponse(null, null, null, null),
            CreatedAt: DateTime.UtcNow);

        return CreatedAtAction(
            nameof(GetTenant),
            new { tenantId = result.TenantId },
            response);
    }

    /// <summary>
    /// Retrieves the details of a tenant by its unique identifier.
    /// </summary>
    /// <param name="tenantId">The identifier of the tenant to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the tenant details, or 404 Not Found.</returns>
    [HttpGet("{tenantId:guid}")]
    [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTenant(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetTenantQuery(tenantId), cancellationToken);

        if (result is null)
            return NotFound();

        var response = new TenantResponse(
            Id: result.Id,
            Name: result.Name,
            Slug: result.Slug,
            Status: result.Status,
            BillingStatus: result.BillingStatus,
            TrialEndsAt: result.TrialEndsAt,
            Branding: new TenantBrandingResponse(
                result.Branding.LogoUrl,
                result.Branding.PrimaryColor,
                result.Branding.SecondaryColor,
                result.Branding.CustomDomain),
            CreatedAt: result.CreatedAt);

        return Ok(response);
    }

    /// <summary>
    /// Updates the visual branding configuration for a tenant.
    /// Accepts multipart/form-data with an optional logo file and branding parameters.
    /// Only non-null values are applied; existing values are preserved.
    /// </summary>
    /// <param name="tenantId">The identifier of the tenant.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the updated branding configuration.</returns>
    [HttpPatch("{tenantId:guid}/branding")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(TenantBrandingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBranding(
        Guid tenantId,
        IFormFile? logo,
        [FromForm] string? primaryColor,
        [FromForm] string? secondaryColor,
        [FromForm] string? customDomain,
        CancellationToken cancellationToken)
    {
        Stream? logoStream = null;
        string? logoContentType = null;

        if (logo is not null)
        {
            logoStream = logo.OpenReadStream();
            logoContentType = logo.ContentType;
        }

        try
        {
            var command = new UpdateTenantBrandingCommand(
                tenantId,
                logoStream,
                logoContentType,
                primaryColor,
                secondaryColor,
                customDomain);

            var result = await _sender.Send(command, cancellationToken);

            var response = new TenantBrandingResponse(
                result.LogoUrl,
                result.PrimaryColor,
                result.SecondaryColor,
                result.CustomDomain);

            return Ok(response);
        }
        finally
        {
            if (logoStream is not null)
                await logoStream.DisposeAsync();
        }
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
