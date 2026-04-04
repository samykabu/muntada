using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.SharedKernel.Application;
using Muntada.Tenancy.Domain.Events;
using Muntada.Tenancy.Domain.Membership;
using Muntada.Tenancy.Infrastructure;

namespace Muntada.Tenancy.Application.Commands;

/// <summary>
/// Command to update the role of an existing tenant member.
/// Only tenant Owners may change member roles.
/// </summary>
/// <param name="TenantId">The identifier of the tenant.</param>
/// <param name="MembershipId">The identifier of the membership to update.</param>
/// <param name="NewRole">The new role to assign to the member.</param>
/// <param name="RequestedBy">The identifier of the user requesting the role change.</param>
public sealed record UpdateTenantMemberRoleCommand(
    Guid TenantId,
    Guid MembershipId,
    TenantRole NewRole,
    Guid RequestedBy) : IRequest;

/// <summary>
/// Handles <see cref="UpdateTenantMemberRoleCommand"/> by verifying the requester is an Owner,
/// enforcing the last-Owner guard, updating the role, and publishing a
/// <see cref="TenantMembershipChangedEvent"/> integration event.
/// </summary>
public sealed class UpdateTenantMemberRoleCommandHandler : IRequestHandler<UpdateTenantMemberRoleCommand>
{
    private readonly TenancyDbContext _dbContext;
    private readonly IIntegrationEventPublisher _eventPublisher;

    /// <summary>
    /// Initializes a new instance of <see cref="UpdateTenantMemberRoleCommandHandler"/>.
    /// </summary>
    /// <param name="dbContext">The Tenancy module database context.</param>
    /// <param name="eventPublisher">Publisher for integration events.</param>
    public UpdateTenantMemberRoleCommandHandler(
        TenancyDbContext dbContext,
        IIntegrationEventPublisher eventPublisher)
    {
        _dbContext = dbContext;
        _eventPublisher = eventPublisher;
    }

    /// <summary>
    /// Handles the role update: validates requester is Owner, enforces last-Owner guard,
    /// updates the membership role, persists, and publishes event.
    /// </summary>
    /// <param name="request">The update role command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="FluentValidation.ValidationException">
    /// Thrown when the requester is not an Owner, the membership is not found,
    /// or the last-Owner guard is violated.
    /// </exception>
    public async Task Handle(UpdateTenantMemberRoleCommand request, CancellationToken cancellationToken)
    {
        // 1. Verify requester is Owner
        var requesterMembership = await _dbContext.TenantMemberships
            .FirstOrDefaultAsync(m =>
                m.TenantId == request.TenantId &&
                m.UserId == request.RequestedBy &&
                m.Status == TenantMembershipStatus.Active,
                cancellationToken);

        if (requesterMembership is null || requesterMembership.Role != TenantRole.Owner)
        {
            throw new FluentValidation.ValidationException(
                [new FluentValidation.Results.ValidationFailure("RequestedBy", "Only Owners can change member roles.")]);
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

        // 2. If changing FROM Owner, verify at least 2 Owners exist (last-Owner guard)
        if (membership.Role == TenantRole.Owner && request.NewRole != TenantRole.Owner)
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
                    [new FluentValidation.Results.ValidationFailure("NewRole", "Cannot change the role of the last Owner. Assign another Owner first.")]);
            }
        }

        // 3. Update the role
        membership.UpdateRole(request.NewRole);
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
            Role: request.NewRole.ToString(),
            ChangeType: "RoleUpdated");
        await _eventPublisher.PublishAsync(@event, cancellationToken);
    }
}
