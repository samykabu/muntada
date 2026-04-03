using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.Identity.Domain.Events;
using Muntada.Identity.Domain.Pat;
using Muntada.Identity.Infrastructure;
using Muntada.SharedKernel.Application;

namespace Muntada.Identity.Application.Commands;

/// <summary>
/// Command to revoke a Personal Access Token.
/// </summary>
/// <param name="PatId">The unique identifier of the PAT to revoke.</param>
/// <param name="UserId">The unique identifier of the user who owns the PAT.</param>
public sealed record RevokePatCommand(Guid PatId, Guid UserId) : IRequest<bool>;

/// <summary>
/// Handles <see cref="RevokePatCommand"/> by finding the PAT, verifying ownership,
/// revoking it, and publishing a <see cref="PATRevokedEvent"/>.
/// </summary>
public sealed class RevokePatCommandHandler : IRequestHandler<RevokePatCommand, bool>
{
    private readonly IdentityDbContext _dbContext;
    private readonly IIntegrationEventPublisher _eventPublisher;

    /// <summary>
    /// Initializes a new instance of <see cref="RevokePatCommandHandler"/>.
    /// </summary>
    /// <param name="dbContext">The Identity module database context.</param>
    /// <param name="eventPublisher">Publisher for integration events.</param>
    public RevokePatCommandHandler(
        IdentityDbContext dbContext,
        IIntegrationEventPublisher eventPublisher)
    {
        _dbContext = dbContext;
        _eventPublisher = eventPublisher;
    }

    /// <summary>
    /// Handles PAT revocation: finds the PAT by ID, verifies the requesting user
    /// is the owner, revokes it, and publishes a <see cref="PATRevokedEvent"/>.
    /// </summary>
    /// <param name="request">The revoke PAT command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the PAT was revoked; <c>false</c> if not found or not owned.</returns>
    public async Task<bool> Handle(RevokePatCommand request, CancellationToken cancellationToken)
    {
        var pat = await _dbContext.Set<PersonalAccessToken>()
            .FirstOrDefaultAsync(p => p.Id == request.PatId && p.UserId == request.UserId, cancellationToken);

        if (pat is null || pat.Status != PatStatus.Active)
            return false;

        pat.Revoke();
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Publish integration event
        var @event = PATRevokedEvent.Create(request.UserId, pat.Id);
        await _eventPublisher.PublishAsync(@event, cancellationToken);

        return true;
    }
}
