using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.SharedKernel.Application;
using Muntada.Tenancy.Domain.Events;
using Muntada.Tenancy.Domain.Membership;
using Muntada.Tenancy.Infrastructure;

namespace Muntada.Tenancy.Application.Commands;

/// <summary>
/// Command to remove (deactivate) a member from a tenant.
/// Owners and Admins can remove members, but Admins cannot remove Owners.
/// </summary>
/// <param name="TenantId">The identifier of the tenant.</param>
/// <param name="MembershipId">The identifier of the membership to deactivate.</param>
/// <param name="RequestedBy">The identifier of the user requesting the removal.</param>
public sealed record RemoveTenantMemberCommand(
    Guid TenantId,
    Guid MembershipId,
    Guid RequestedBy) : IRequest;

/// <summary>
/// Handles <see cref="RemoveTenantMemberCommand"/> by verifying requester permissions,
/// enforcing the last-Owner guard, deactivating the membership, and publishing
/// a <see cref="TenantMembershipChangedEvent"/> integration event.
/// </summary>
public sealed class RemoveTenantMemberCommandHandler : IRequestHandler<RemoveTenantMemberCommand>
{
    private readonly TenancyDbContext _dbContext;
    private readonly IIntegrationEventPublisher _eventPublisher;

    /// <summary>
    /// Initializes a new instance of <see cref="RemoveTenantMemberCommandHandler"/>.
    /// </summary>
    /// <param name="dbContext">The Tenancy module database context.</param>
    /// <param name="eventPublisher">Publisher for integration events.</param>
    public RemoveTenantMemberCommandHandler(
        TenancyDbContext dbContext,
        IIntegrationEventPublisher eventPublisher)
    {
        _dbContext = dbContext;
        _eventPublisher = eventPublisher;
    }

    /// <summary>
    /// Handles the member removal: validates requester permissions, enforces last-Owner guard,
    /// deactivates the membership, persists, and publishes event.
    /// </summary>
    /// <param name="request">The remove member command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="FluentValidation.ValidationException">
    /// Thrown when the requester lacks permission, the membership is not found,
    /// or the last-Owner guard is violated.
    /// </exception>
    public async Task Handle(RemoveTenantMemberCommand request, CancellationToken cancellationToken)
    {
        // 1. Verify requester is Owner or Admin
        var requesterMembership = await _dbContext.TenantMemberships
            .FirstOrDefaultAsync(m =>
                m.TenantId == request.TenantId &&
                m.UserId == request.RequestedBy &&
                m.Status == TenantMembershipStatus.Active,
                cancellationToken);

        if (requesterMembership is null)
        {
            throw new FluentValidation.ValidationException(
                [new FluentValidation.Results.ValidationFailure("RequestedBy", "Requester is not an active member of this tenant.")]);
        }

        if (requesterMembership.Role != TenantRole.Owner && requesterMembership.Role != TenantRole.Admin)
        {
            throw new FluentValidation.ValidationException(
                [new FluentValidation.Results.ValidationFailure("RequestedBy", "Only Owners and Admins can remove members.")]);
        }

        // Find the target membership
        var membership = await _dbContext.TenantMemberships
            .FirstOrDefaultAsync(m =>
                m.Id == request.MembershipId &&
                m.TenantId == request.TenantId,
                cancellationToken);

        if (membership is null)
        {
            throw new FluentValidation.ValidationException(
                [new FluentValidation.Results.ValidationFailure("MembershipId", "Membership not found in this tenant.")]);
        }

        // Admins cannot remove Owners
        if (requesterMembership.Role == TenantRole.Admin && membership.Role == TenantRole.Owner)
        {
            throw new FluentValidation.ValidationException(
                [new FluentValidation.Results.ValidationFailure("RequestedBy", "Admins cannot remove Owners.")]);
        }

        // 2. If removing an Owner, verify at least 2 Owners exist
        if (membership.Role == TenantRole.Owner)
        {
            var ownerCount = await _dbContext.TenantMemberships
                .CountAsync(m =>
                    m.TenantId == request.TenantId &&
                    m.Role == TenantRole.Owner &&
                    m.Status == TenantMembershipStatus.Active,
                    cancellationToken);

            if (ownerCount < 2)
            {
                throw new FluentValidation.ValidationException(
                    [new FluentValidation.Results.ValidationFailure("MembershipId", "Cannot remove the last Owner. Assign another Owner first.")]);
            }
        }

        // 3. Deactivate the membership
        membership.Deactivate();
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 4. Publish integration event
        var @event = new TenantMembershipChangedEvent(
            EventId: Guid.NewGuid(),
            OccurredAt: DateTimeOffset.UtcNow,
            AggregateId: request.TenantId.ToString(),
            AggregateType: nameof(TenantMembership),
            Version: 1,
            TenantId: request.TenantId,
            MembershipId: membership.Id,
            UserId: membership.UserId,
            Role: membership.Role.ToString(),
            ChangeType: "Deactivated");
        await _eventPublisher.PublishAsync(@event, cancellationToken);
    }
}
