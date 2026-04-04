using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.SharedKernel.Application;
using Muntada.Tenancy.Domain.Events;
using Muntada.Tenancy.Domain.Membership;
using Muntada.Tenancy.Infrastructure;

namespace Muntada.Tenancy.Application.Commands;

/// <summary>
/// Command to accept a pending tenant membership invitation using an invite token.
/// </summary>
/// <param name="Token">The invite token string received via email.</param>
/// <param name="UserId">The identifier of the user accepting the invitation.</param>
public sealed record AcceptTenantInviteCommand(
    string Token,
    Guid UserId) : IRequest<AcceptInviteResult>;

/// <summary>
/// Result returned after successfully accepting a tenant membership invitation.
/// </summary>
/// <param name="TenantId">The identifier of the tenant the user has joined.</param>
/// <param name="MembershipId">The identifier of the activated membership.</param>
/// <param name="Role">The role assigned to the user within the tenant.</param>
public sealed record AcceptInviteResult(
    Guid TenantId,
    Guid MembershipId,
    string Role);

/// <summary>
/// Handles <see cref="AcceptTenantInviteCommand"/> by validating the invite token,
/// enforcing the maximum tenants-per-user limit, activating the membership,
/// marking the token as used, and publishing a <see cref="TenantMembershipChangedEvent"/>.
/// </summary>
public sealed class AcceptTenantInviteCommandHandler : IRequestHandler<AcceptTenantInviteCommand, AcceptInviteResult>
{
    /// <summary>
    /// The maximum number of active tenant memberships a single user can hold.
    /// </summary>
    private const int MaxTenantsPerUser = 10;

    private readonly TenancyDbContext _dbContext;
    private readonly IIntegrationEventPublisher _eventPublisher;

    /// <summary>
    /// Initializes a new instance of <see cref="AcceptTenantInviteCommandHandler"/>.
    /// </summary>
    /// <param name="dbContext">The Tenancy module database context.</param>
    /// <param name="eventPublisher">Publisher for integration events.</param>
    public AcceptTenantInviteCommandHandler(
        TenancyDbContext dbContext,
        IIntegrationEventPublisher eventPublisher)
    {
        _dbContext = dbContext;
        _eventPublisher = eventPublisher;
    }

    /// <summary>
    /// Handles the invite acceptance: validates token, enforces membership limits,
    /// activates membership, marks token as used, persists, and publishes event.
    /// </summary>
    /// <param name="request">The accept invite command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The acceptance result containing tenant and membership details.</returns>
    /// <exception cref="FluentValidation.ValidationException">
    /// Thrown when the token is invalid, expired, already used, or the user has
    /// reached the maximum number of active tenant memberships.
    /// </exception>
    public async Task<AcceptInviteResult> Handle(AcceptTenantInviteCommand request, CancellationToken cancellationToken)
    {
        // 1. Find token by value, validate not expired/used
        var token = await _dbContext.TenantInviteTokens
            .FirstOrDefaultAsync(t => t.Token == request.Token, cancellationToken);

        if (token is null)
        {
            throw new FluentValidation.ValidationException(
                [new FluentValidation.Results.ValidationFailure("Token", "Invalid invite token.")]);
        }

        if (token.IsUsed)
        {
            throw new FluentValidation.ValidationException(
                [new FluentValidation.Results.ValidationFailure("Token", "This invite token has already been used.")]);
        }

        if (token.IsExpired())
        {
            throw new FluentValidation.ValidationException(
                [new FluentValidation.Results.ValidationFailure("Token", "This invite token has expired.")]);
        }

        // 2. Count user's active memberships across all tenants
        var activeMembershipCount = await _dbContext.TenantMemberships
            .CountAsync(m =>
                m.UserId == request.UserId &&
                m.Status == TenantMembershipStatus.Active,
                cancellationToken);

        if (activeMembershipCount >= MaxTenantsPerUser)
        {
            throw new FluentValidation.ValidationException(
                [new FluentValidation.Results.ValidationFailure("UserId", $"User has reached the maximum of {MaxTenantsPerUser} active tenant memberships.")]);
        }

        // 3. Find associated membership
        var membership = await _dbContext.TenantMemberships
            .FirstOrDefaultAsync(m => m.Id == token.MembershipId, cancellationToken);

        if (membership is null)
        {
            throw new FluentValidation.ValidationException(
                [new FluentValidation.Results.ValidationFailure("Token", "The membership associated with this token no longer exists.")]);
        }

        // 4. Accept the invite
        membership.AcceptInvite(request.UserId);

        // 5. Mark token as used
        token.MarkAsUsed();

        // 6. Save changes
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 7. Publish integration event
        var @event = new TenantMembershipChangedEvent(
            EventId: Guid.NewGuid(),
            OccurredAt: DateTimeOffset.UtcNow,
            AggregateId: membership.TenantId.ToString(),
            AggregateType: nameof(TenantMembership),
            Version: 1,
            TenantId: membership.TenantId,
            MembershipId: membership.Id,
            UserId: request.UserId,
            Role: membership.Role.ToString(),
            ChangeType: "Accepted");
        await _eventPublisher.PublishAsync(@event, cancellationToken);

        // 8. Return result
        return new AcceptInviteResult(
            TenantId: membership.TenantId,
            MembershipId: membership.Id,
            Role: membership.Role.ToString());
    }
}
