using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.SharedKernel.Application;
using Muntada.Tenancy.Domain.Events;
using Muntada.Tenancy.Domain.Retention;
using Muntada.Tenancy.Infrastructure;

namespace Muntada.Tenancy.Application.Commands;

/// <summary>
/// Command to update a tenant's data retention policy.
/// Only non-null values are applied; existing values are preserved when <c>null</c>.
/// </summary>
/// <param name="TenantId">The identifier of the tenant whose retention policy should be updated.</param>
/// <param name="RecordingDays">New recording retention in days, or <c>null</c> to keep current value.</param>
/// <param name="ChatDays">New chat message retention in days, or <c>null</c> to keep current value.</param>
/// <param name="FileDays">New file retention in days, or <c>null</c> to keep current value.</param>
/// <param name="AuditLogDays">New audit log retention in days, or <c>null</c> to keep current value.</param>
/// <param name="ActivityDays">New user activity log retention in days, or <c>null</c> to keep current value.</param>
public sealed record UpdateRetentionPolicyCommand(
    Guid TenantId,
    int? RecordingDays,
    int? ChatDays,
    int? FileDays,
    int? AuditLogDays,
    int? ActivityDays) : IRequest<UpdateRetentionPolicyResult>;

/// <summary>
/// Result returned after a successful retention policy update.
/// </summary>
/// <param name="TenantId">The identifier of the tenant.</param>
/// <param name="RecordingRetentionDays">The updated recording retention period in days.</param>
/// <param name="ChatMessageRetentionDays">The updated chat message retention period in days.</param>
/// <param name="FileRetentionDays">The updated file retention period in days.</param>
/// <param name="AuditLogRetentionDays">The updated audit log retention period in days.</param>
/// <param name="UserActivityLogRetentionDays">The updated user activity log retention period in days.</param>
/// <param name="UpdatedAt">The UTC timestamp when the policy was last updated.</param>
public sealed record UpdateRetentionPolicyResult(
    Guid TenantId,
    int RecordingRetentionDays,
    int ChatMessageRetentionDays,
    int FileRetentionDays,
    int AuditLogRetentionDays,
    int UserActivityLogRetentionDays,
    DateTime UpdatedAt);

/// <summary>
/// Handles <see cref="UpdateRetentionPolicyCommand"/> by finding the tenant's retention policy,
/// applying the requested changes, publishing a <see cref="RetentionPolicyChangedEvent"/>,
/// and persisting the updated policy.
/// </summary>
public sealed class UpdateRetentionPolicyCommandHandler
    : IRequestHandler<UpdateRetentionPolicyCommand, UpdateRetentionPolicyResult>
{
    private readonly TenancyDbContext _dbContext;
    private readonly IIntegrationEventPublisher _eventPublisher;

    /// <summary>
    /// Initializes a new instance of <see cref="UpdateRetentionPolicyCommandHandler"/>.
    /// </summary>
    /// <param name="dbContext">The Tenancy module database context.</param>
    /// <param name="eventPublisher">Publisher for integration events.</param>
    public UpdateRetentionPolicyCommandHandler(
        TenancyDbContext dbContext,
        IIntegrationEventPublisher eventPublisher)
    {
        _dbContext = dbContext;
        _eventPublisher = eventPublisher;
    }

    /// <summary>
    /// Handles the retention policy update: finds the policy, applies changes,
    /// publishes the <see cref="RetentionPolicyChangedEvent"/>, and persists.
    /// </summary>
    /// <param name="request">The retention policy update command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated retention policy details.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no retention policy exists for the specified tenant.
    /// </exception>
    public async Task<UpdateRetentionPolicyResult> Handle(
        UpdateRetentionPolicyCommand request,
        CancellationToken cancellationToken)
    {
        var policy = await _dbContext.RetentionPolicies
            .FirstOrDefaultAsync(r => r.TenantId == request.TenantId, cancellationToken)
            ?? throw new InvalidOperationException(
                $"No retention policy found for tenant '{request.TenantId}'.");

        policy.Update(
            request.RecordingDays,
            request.ChatDays,
            request.FileDays,
            request.AuditLogDays,
            request.ActivityDays);

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Publish integration event
        var @event = new RetentionPolicyChangedEvent(
            EventId: Guid.NewGuid(),
            OccurredAt: DateTimeOffset.UtcNow,
            AggregateId: policy.Id.ToString(),
            AggregateType: nameof(RetentionPolicy),
            Version: 1,
            TenantId: request.TenantId,
            RecordingRetentionDays: policy.RecordingRetentionDays,
            ChatMessageRetentionDays: policy.ChatMessageRetentionDays,
            FileRetentionDays: policy.FileRetentionDays,
            AuditLogRetentionDays: policy.AuditLogRetentionDays,
            UserActivityLogRetentionDays: policy.UserActivityLogRetentionDays);
        await _eventPublisher.PublishAsync(@event, cancellationToken);

        return new UpdateRetentionPolicyResult(
            TenantId: request.TenantId,
            RecordingRetentionDays: policy.RecordingRetentionDays,
            ChatMessageRetentionDays: policy.ChatMessageRetentionDays,
            FileRetentionDays: policy.FileRetentionDays,
            AuditLogRetentionDays: policy.AuditLogRetentionDays,
            UserActivityLogRetentionDays: policy.UserActivityLogRetentionDays,
            UpdatedAt: policy.UpdatedAt);
    }
}
