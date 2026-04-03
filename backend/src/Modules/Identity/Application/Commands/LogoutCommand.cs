using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.Identity.Domain.Events;
using Muntada.Identity.Domain.Session;
using Muntada.Identity.Infrastructure;
using Muntada.SharedKernel.Application;

namespace Muntada.Identity.Application.Commands;

/// <summary>
/// Command to log out a user by revoking their current session.
/// </summary>
/// <param name="SessionId">The unique identifier of the session to terminate.</param>
/// <param name="UserId">The unique identifier of the user logging out.</param>
public sealed record LogoutCommand(Guid SessionId, Guid UserId) : IRequest<bool>;

/// <summary>
/// Handles <see cref="LogoutCommand"/> by revoking the session and its refresh token,
/// then publishing a <see cref="UserLoggedOutEvent"/>.
/// </summary>
public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, bool>
{
    private readonly IdentityDbContext _dbContext;
    private readonly IIntegrationEventPublisher _eventPublisher;

    /// <summary>
    /// Initializes a new instance of <see cref="LogoutCommandHandler"/>.
    /// </summary>
    /// <param name="dbContext">The Identity module database context.</param>
    /// <param name="eventPublisher">Publisher for integration events.</param>
    public LogoutCommandHandler(
        IdentityDbContext dbContext,
        IIntegrationEventPublisher eventPublisher)
    {
        _dbContext = dbContext;
        _eventPublisher = eventPublisher;
    }

    /// <summary>
    /// Handles user logout: revokes the session and its refresh token,
    /// persists changes, and publishes a <see cref="UserLoggedOutEvent"/>.
    /// </summary>
    /// <param name="request">The logout command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the session was revoked; <c>false</c> if not found or already revoked.</returns>
    public async Task<bool> Handle(LogoutCommand request, CancellationToken cancellationToken)
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
        var @event = UserLoggedOutEvent.Create(request.UserId, session.Id);
        await _eventPublisher.PublishAsync(@event, cancellationToken);

        return true;
    }
}
