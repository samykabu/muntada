using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.Tenancy.Domain.Retention;
using Muntada.Tenancy.Infrastructure;

namespace Muntada.Tenancy.Application.Queries;

/// <summary>
/// Query to retrieve a tenant's data retention policy with current values and allowed ranges.
/// </summary>
/// <param name="TenantId">The identifier of the tenant whose retention policy to retrieve.</param>
public sealed record GetRetentionPolicyQuery(Guid TenantId) : IRequest<RetentionPolicyResult?>;

/// <summary>
/// Result containing the current retention policy values and allowed configuration ranges.
/// </summary>
/// <param name="TenantId">The identifier of the tenant.</param>
/// <param name="RecordingRetentionDays">Current recording retention in days.</param>
/// <param name="ChatMessageRetentionDays">Current chat message retention in days.</param>
/// <param name="FileRetentionDays">Current file retention in days.</param>
/// <param name="AuditLogRetentionDays">Current audit log retention in days.</param>
/// <param name="UserActivityLogRetentionDays">Current user activity log retention in days.</param>
/// <param name="UpdatedAt">UTC timestamp of the last policy update.</param>
/// <param name="AllowedRange">The allowed range for retention values.</param>
public sealed record RetentionPolicyResult(
    Guid TenantId,
    int RecordingRetentionDays,
    int ChatMessageRetentionDays,
    int FileRetentionDays,
    int AuditLogRetentionDays,
    int UserActivityLogRetentionDays,
    DateTime UpdatedAt,
    RetentionAllowedRange AllowedRange);

/// <summary>
/// Describes the allowed ranges for retention policy values.
/// </summary>
/// <param name="MinDays">The minimum allowed retention period in days.</param>
/// <param name="MaxDays">The maximum allowed retention period in days.</param>
/// <param name="MinAuditLogDays">The minimum allowed audit log retention period in days.</param>
public sealed record RetentionAllowedRange(
    int MinDays,
    int MaxDays,
    int MinAuditLogDays);

/// <summary>
/// Handles <see cref="GetRetentionPolicyQuery"/> by querying the database for the tenant's
/// retention policy and returning it with allowed configuration ranges.
/// Returns <c>null</c> when no policy is found.
/// </summary>
public sealed class GetRetentionPolicyQueryHandler
    : IRequestHandler<GetRetentionPolicyQuery, RetentionPolicyResult?>
{
    private readonly TenancyDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of <see cref="GetRetentionPolicyQueryHandler"/>.
    /// </summary>
    /// <param name="dbContext">The Tenancy module database context.</param>
    public GetRetentionPolicyQueryHandler(TenancyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Handles the query by looking up the retention policy and projecting to a result
    /// that includes allowed ranges.
    /// </summary>
    /// <param name="request">The retention policy query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The retention policy result, or <c>null</c> if not found.</returns>
    public async Task<RetentionPolicyResult?> Handle(
        GetRetentionPolicyQuery request,
        CancellationToken cancellationToken)
    {
        var policy = await _dbContext.RetentionPolicies
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.TenantId == request.TenantId, cancellationToken);

        if (policy is null)
            return null;

        return new RetentionPolicyResult(
            TenantId: policy.TenantId,
            RecordingRetentionDays: policy.RecordingRetentionDays,
            ChatMessageRetentionDays: policy.ChatMessageRetentionDays,
            FileRetentionDays: policy.FileRetentionDays,
            AuditLogRetentionDays: policy.AuditLogRetentionDays,
            UserActivityLogRetentionDays: policy.UserActivityLogRetentionDays,
            UpdatedAt: policy.UpdatedAt,
            AllowedRange: new RetentionAllowedRange(
                MinDays: RetentionPolicy.MinRetentionDays,
                MaxDays: RetentionPolicy.MaxRetentionDays,
                MinAuditLogDays: RetentionPolicy.MinAuditLogRetentionDays));
    }
}
