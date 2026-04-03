using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.Identity.Domain.Session;
using Muntada.Identity.Infrastructure;

namespace Muntada.Identity.Application.Queries;

/// <summary>
/// Query to retrieve all sessions for a given user.
/// </summary>
/// <param name="UserId">The unique identifier of the user whose sessions are requested.</param>
public sealed record ListSessionsQuery(Guid UserId) : IRequest<List<SessionDto>>;

/// <summary>
/// DTO representing a user session.
/// </summary>
/// <param name="SessionId">The unique identifier of the session.</param>
/// <param name="DeviceUserAgent">The User-Agent string of the session's device.</param>
/// <param name="DeviceIpAddress">The IP address of the session's device.</param>
/// <param name="CreatedAt">The UTC timestamp when the session was created.</param>
/// <param name="LastActivityAt">The UTC timestamp of the last activity on this session.</param>
/// <param name="IsCurrent">Whether this is the current session making the request.</param>
public sealed record SessionDto(
    Guid SessionId,
    string DeviceUserAgent,
    string DeviceIpAddress,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastActivityAt,
    bool IsCurrent);

/// <summary>
/// Handles <see cref="ListSessionsQuery"/> by retrieving all sessions for the specified user
/// and mapping them to <see cref="SessionDto"/> records.
/// </summary>
public sealed class ListSessionsQueryHandler : IRequestHandler<ListSessionsQuery, List<SessionDto>>
{
    private readonly IdentityDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of <see cref="ListSessionsQueryHandler"/>.
    /// </summary>
    /// <param name="dbContext">The Identity module database context.</param>
    public ListSessionsQueryHandler(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Handles the query by fetching all active sessions for the user and projecting to DTOs.
    /// </summary>
    /// <param name="request">The list sessions query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of session DTOs for the specified user.</returns>
    public async Task<List<SessionDto>> Handle(ListSessionsQuery request, CancellationToken cancellationToken)
    {
        var sessions = await _dbContext.Set<Session>()
            .Where(s => s.UserId == request.UserId && s.Status == SessionStatus.Active)
            .OrderByDescending(s => s.LastActivityAt)
            .ToListAsync(cancellationToken);

        return sessions.Select(s => new SessionDto(
            s.Id,
            s.DeviceInfo.UserAgent,
            s.DeviceInfo.IpAddress,
            s.CreatedAt,
            s.LastActivityAt,
            IsCurrent: false // Caller must set the current session flag
        )).ToList();
    }
}
