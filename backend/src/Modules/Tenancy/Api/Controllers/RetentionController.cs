using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Muntada.Tenancy.Api.Dtos;
using Muntada.Tenancy.Application.Commands;
using Muntada.Tenancy.Application.Queries;

namespace Muntada.Tenancy.Api.Controllers;

/// <summary>
/// Handles data retention policy operations for tenants.
/// Provides endpoints to view and update retention configurations.
/// </summary>
[ApiController]
[Route("api/v1/tenants/{tenantId:guid}/retention-policies")]
public sealed class RetentionController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes a new instance of <see cref="RetentionController"/>.
    /// </summary>
    /// <param name="sender">MediatR sender for dispatching commands and queries.</param>
    public RetentionController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Retrieves the current data retention policy for a tenant,
    /// including default values and allowed configuration ranges.
    /// </summary>
    /// <param name="tenantId">The identifier of the tenant.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the retention policy, or 404 Not Found.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(RetentionPolicyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRetentionPolicy(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetRetentionPolicyQuery(tenantId), cancellationToken);

        if (result is null)
            return NotFound();

        var response = new RetentionPolicyResponse(
            TenantId: result.TenantId,
            RecordingRetentionDays: result.RecordingRetentionDays,
            ChatMessageRetentionDays: result.ChatMessageRetentionDays,
            FileRetentionDays: result.FileRetentionDays,
            AuditLogRetentionDays: result.AuditLogRetentionDays,
            UserActivityLogRetentionDays: result.UserActivityLogRetentionDays,
            UpdatedAt: result.UpdatedAt,
            AllowedRange: new RetentionAllowedRangeResponse(
                MinDays: result.AllowedRange.MinDays,
                MaxDays: result.AllowedRange.MaxDays,
                MinAuditLogDays: result.AllowedRange.MinAuditLogDays));

        return Ok(response);
    }

    /// <summary>
    /// Updates the data retention policy for a tenant.
    /// Only non-null values in the request are applied; existing values are preserved.
    /// </summary>
    /// <param name="tenantId">The identifier of the tenant.</param>
    /// <param name="request">The retention policy update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the updated retention policy.</returns>
    [HttpPatch]
    [ProducesResponseType(typeof(RetentionPolicyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRetentionPolicy(
        Guid tenantId,
        [FromBody] UpdateRetentionPolicyRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateRetentionPolicyCommand(
            tenantId,
            request.RecordingDays,
            request.ChatDays,
            request.FileDays,
            request.AuditLogDays,
            request.ActivityDays);

        var result = await _sender.Send(command, cancellationToken);

        var response = new RetentionPolicyResponse(
            TenantId: result.TenantId,
            RecordingRetentionDays: result.RecordingRetentionDays,
            ChatMessageRetentionDays: result.ChatMessageRetentionDays,
            FileRetentionDays: result.FileRetentionDays,
            AuditLogRetentionDays: result.AuditLogRetentionDays,
            UserActivityLogRetentionDays: result.UserActivityLogRetentionDays,
            UpdatedAt: result.UpdatedAt,
            AllowedRange: new RetentionAllowedRangeResponse(
                MinDays: Domain.Retention.RetentionPolicy.MinRetentionDays,
                MaxDays: Domain.Retention.RetentionPolicy.MaxRetentionDays,
                MinAuditLogDays: Domain.Retention.RetentionPolicy.MinAuditLogRetentionDays));

        return Ok(response);
    }
}
