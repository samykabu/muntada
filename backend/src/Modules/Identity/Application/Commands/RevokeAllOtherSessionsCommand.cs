using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.Identity.Domain.Session;
using Muntada.Identity.Infrastructure;

namespace Muntada.Identity.Application.Commands;

/// <summary>
/// Command to revoke all active sessions for a user except the current session.
/// </summary>
/// <param name="CurrentSessionId">The session to keep active.</param>
/// <param name="UserId">The unique identifier of the user whose other sessions should be revoked.</param>
public sealed record RevokeAllOtherSessionsCommand(Guid CurrentSessionId, Guid UserId) : IRequest<int>;

/// <summary>
/// Handles <see cref="RevokeAllOtherSessionsCommand"/> by finding all active sessions
/// except the current one and revoking them along with their refresh tokens.
/// </summary>
public sealed class RevokeAllOtherSessionsCommandHandler : IRequestHandler<RevokeAllOtherSessionsCommand, int>
{
    private readonly IdentityDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of <see cref="RevokeAllOtherSessionsCommandHandler"/>.
    /// </summary>
    /// <param name="dbContext">The Identity module database context.</param>
    public RevokeAllOtherSessionsCommandHandler(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Handles revoking all active sessions except the current one.
    /// Also revokes the bound refresh tokens for each revoked session.
    /// </summary>
    /// <param name="request">The revoke all other sessions command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of sessions that were revoked.</returns>
    public async Task<int> Handle(RevokeAllOtherSessionsCommand request, CancellationToken cancellationToken)
    {
        var otherSessions = await _dbContext.Set<Session>()
            .Where(s => s.UserId == request.UserId
                && s.Id != request.CurrentSessionId
                && s.Status == SessionStatus.Active)
            .ToListAsync(cancellationToken);

        if (otherSessions.Count == 0)
            return 0;

        var sessionIds = otherSessions.Select(s => s.Id).ToList();

        // Revoke all other sessions
        foreach (var session in otherSessions)
        {
            session.Revoke();
        }

        // Revoke all bound refresh tokens
        var refreshTokens = await _dbContext.Set<RefreshToken>()
            .Where(rt => sessionIds.Contains(rt.SessionId) && rt.Status == RefreshTokenStatus.Active)
            .ToListAsync(cancellationToken);

        foreach (var token in refreshTokens)
        {
            token.Revoke();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return otherSessions.Count;
    }
}
