using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.Identity.Domain.Events;
using Muntada.Identity.Domain.Session;
using Muntada.Identity.Infrastructure;
using Muntada.SharedKernel.Application;

namespace Muntada.Identity.Application.Commands;

/// <summary>
/// Command to revoke a specific user session.
/// </summary>
/// <param name="SessionId">The unique identifier of the session to revoke.</param>
/// <param name="UserId">The unique identifier of the user who owns the session.</param>
public sealed record RevokeSessionCommand(Guid SessionId, Guid UserId) : IRequest<bool>;

/// <summary>
/// Handles <see cref="RevokeSessionCommand"/> by finding the session, verifying ownership,
/// revoking it and its bound refresh token, and publishing a <see cref="SessionRevokedEvent"/>.
/// </summary>
public sealed class RevokeSessionCommandHandler : IRequestHandler<RevokeSessionCommand, bool>
{
    private readonly IdentityDbContext _dbContext;
    private readonly IIntegrationEventPublisher _eventPublisher;

    /// <summary>
    /// Initializes a new instance of <see cref="RevokeSessionCommandHandler"/>.
    /// </summary>
    /// <param name="dbContext">The Identity module database context.</param>
    /// <param name="eventPublisher">Publisher for integration events.</param>
    public RevokeSessionCommandHandler(
        IdentityDbContext dbContext,
        IIntegrationEventPublisher eventPublisher)
    {
        _dbContext = dbContext;
        _eventPublisher = eventPublisher;
    }

    /// <summary>
    /// Handles session revocation: verifies ownership, revokes the session and its refresh token,
    /// persists changes, and publishes a <see cref="SessionRevokedEvent"/>.
    /// </summary>
    /// <param name="request">The revoke session command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the session was revoked; <c>false</c> if not found or not owned.</returns>
    public async Task<bool> Handle(RevokeSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _dbContext.Set<Session>()
            .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.UserId == request.UserId, cancellationToken);

        if (session is null || session.Status != SessionStatus.Active)
            return false;

        // Revoke the session
        session.Revoke();

        // Revoke the bound refresh token
        var refreshToken = await _dbContext.Set<RefreshToken>()
            .FirstOrDefaultAsync(rt => rt.SessionId == session.Id && rt.Status == RefreshTokenStatus.Active, cancellationToken);

        refreshToken?.Revoke();

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Publish integration event
        var @event = SessionRevokedEvent.Create(request.UserId, session.Id);
        await _eventPublisher.PublishAsync(@event, cancellationToken);

        return true;
    }
}
