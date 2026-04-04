using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Muntada.SharedKernel.Application;
using Muntada.Tenancy.Domain.Events;
using Muntada.Tenancy.Domain.Membership;
using Muntada.Tenancy.Infrastructure;

namespace Muntada.Tenancy.Application.Commands;

/// <summary>
/// Command to invite a new member to a tenant via email.
/// </summary>
/// <param name="TenantId">The identifier of the tenant to invite the member to.</param>
/// <param name="Email">The email address of the user to invite.</param>
/// <param name="Role">The role to assign to the invited member upon acceptance.</param>
/// <param name="InvitedBy">The identifier of the user sending the invitation.</param>
/// <param name="Message">Optional personal message to include in the invitation.</param>
public sealed record InviteTenantMemberCommand(
    Guid TenantId,
    string Email,
    TenantRole Role,
    Guid InvitedBy,
    string? Message) : IRequest<InviteMemberResult>;

/// <summary>
/// Result returned after successfully creating a tenant membership invitation.
/// </summary>
/// <param name="MembershipId">The unique identifier of the created membership.</param>
/// <param name="Email">The email address the invitation was sent to.</param>
/// <param name="Role">The role that will be assigned upon acceptance.</param>
/// <param name="Status">The status of the membership (Pending).</param>
/// <param name="ExpiresAt">The UTC date and time when the invitation token expires.</param>
public sealed record InviteMemberResult(
    Guid MembershipId,
    string Email,
    string Role,
    string Status,
    DateTime ExpiresAt);

/// <summary>
/// Handles <see cref="InviteTenantMemberCommand"/> by verifying inviter permissions,
/// checking for existing memberships, creating the invite membership and token,
/// and publishing a <see cref="TenantMembershipChangedEvent"/> integration event.
/// </summary>
public sealed class InviteTenantMemberCommandHandler : IRequestHandler<InviteTenantMemberCommand, InviteMemberResult>
{
    private readonly TenancyDbContext _dbContext;
    private readonly IIntegrationEventPublisher _eventPublisher;
    private readonly ILogger<InviteTenantMemberCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="InviteTenantMemberCommandHandler"/>.
    /// </summary>
    /// <param name="dbContext">The Tenancy module database context.</param>
    /// <param name="eventPublisher">Publisher for integration events.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public InviteTenantMemberCommandHandler(
        TenancyDbContext dbContext,
        IIntegrationEventPublisher eventPublisher,
        ILogger<InviteTenantMemberCommandHandler> logger)
    {
        _dbContext = dbContext;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    /// <summary>
    /// Handles the invitation flow: validates inviter role, checks for duplicates,
    /// cancels stale invites, creates membership and token, persists, and publishes event.
    /// </summary>
    /// <param name="request">The invite command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The invitation result containing membership details and token expiry.</returns>
    /// <exception cref="FluentValidation.ValidationException">
    /// Thrown when the inviter lacks permission, the email is already an active member,
    /// or other business rule violations occur.
    /// </exception>
    public async Task<InviteMemberResult> Handle(InviteTenantMemberCommand request, CancellationToken cancellationToken)
    {
        // 1. Verify inviter has Owner or Admin role
        var inviterMembership = await _dbContext.TenantMemberships
            .FirstOrDefaultAsync(m =>
                m.TenantId == request.TenantId &&
                m.UserId == request.InvitedBy &&
                m.Status == TenantMembershipStatus.Active,
                cancellationToken);

        if (inviterMembership is null)
        {
            throw new FluentValidation.ValidationException(
                [new FluentValidation.Results.ValidationFailure("InvitedBy", "Inviter is not an active member of this tenant.")]);
        }

        if (inviterMembership.Role != TenantRole.Owner && inviterMembership.Role != TenantRole.Admin)
        {
            throw new FluentValidation.ValidationException(
                [new FluentValidation.Results.ValidationFailure("InvitedBy", "Only Owners and Admins can invite members.")]);
        }

        // Admins cannot invite as Owner
        if (inviterMembership.Role == TenantRole.Admin && request.Role == TenantRole.Owner)
        {
            throw new FluentValidation.ValidationException(
                [new FluentValidation.Results.ValidationFailure("Role", "Admins cannot invite members with the Owner role.")]);
        }

        // 2. Check if email already has an active membership in this tenant
        var existingActive = await _dbContext.TenantMemberships
            .AnyAsync(m =>
                m.TenantId == request.TenantId &&
                m.InvitedEmail == request.Email &&
                m.Status == TenantMembershipStatus.Active,
                cancellationToken);

        if (existingActive)
        {
            throw new FluentValidation.ValidationException(
                [new FluentValidation.Results.ValidationFailure("Email", $"The email '{request.Email}' already has an active membership in this tenant.")]);
        }

        // 3. Cancel any existing pending invite for the same email
        var pendingInvites = await _dbContext.TenantMemberships
            .Where(m =>
                m.TenantId == request.TenantId &&
                m.InvitedEmail == request.Email &&
                m.Status == TenantMembershipStatus.Pending)
            .ToListAsync(cancellationToken);

        foreach (var pending in pendingInvites)
        {
            pending.Deactivate();
        }

        // 4. Create new invite membership
        var membership = TenantMembership.CreateInvite(
            request.TenantId,
            request.Email,
            request.Role,
            request.InvitedBy);

        // 5. Generate invite token
        var token = TenantInviteToken.Generate(membership.Id);

        // 6. Persist to DB
        _dbContext.TenantMemberships.Add(membership);
        _dbContext.TenantInviteTokens.Add(token);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 7. TODO: Send invitation email — for now, log it
        _logger.LogInformation(
            "Invitation created for {Email} to tenant {TenantId} with role {Role}. Token expires at {ExpiresAt}.",
            request.Email, request.TenantId, request.Role, token.ExpiresAt);

        // 8. Publish integration event
        var @event = new TenantMembershipChangedEvent(
            EventId: Guid.NewGuid(),
            OccurredAt: DateTimeOffset.UtcNow,
            AggregateId: request.TenantId.ToString(),
            AggregateType: nameof(TenantMembership),
            Version: 1,
            TenantId: request.TenantId,
            MembershipId: membership.Id,
            UserId: null,
            Role: request.Role.ToString(),
            ChangeType: "Invited");
        await _eventPublisher.PublishAsync(@event, cancellationToken);

        // 9. Return result
        return new InviteMemberResult(
            MembershipId: membership.Id,
            Email: request.Email,
            Role: request.Role.ToString(),
            Status: membership.Status.ToString(),
            ExpiresAt: token.ExpiresAt);
    }
}
