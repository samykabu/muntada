using Microsoft.EntityFrameworkCore;
using Muntada.Identity.Domain.Session;
using Muntada.Identity.Infrastructure;

namespace Muntada.Identity.Application.BackgroundJobs;

/// <summary>
/// Background job that finds sessions with no activity in the last 24 hours
/// and marks them as expired. Should be executed on a recurring schedule.
/// </summary>
public sealed class IdleSessionCleanupJob
{
    private static readonly TimeSpan IdleThreshold = TimeSpan.FromHours(24);

    private readonly IdentityDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of <see cref="IdleSessionCleanupJob"/>.
    /// </summary>
    /// <param name="dbContext">The Identity module database context.</param>
    public IdleSessionCleanupJob(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Executes the cleanup by finding all active sessions whose last activity
    /// exceeds the 24-hour idle threshold and marking them as expired.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of sessions that were marked as expired.</returns>
    public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTimeOffset.UtcNow.Subtract(IdleThreshold);

        var idleSessions = await _dbContext.Set<Session>()
            .Where(s => s.Status == SessionStatus.Active
                && (s.LastActivityAt == null || s.LastActivityAt < cutoff))
            .ToListAsync(cancellationToken);

        foreach (var session in idleSessions)
        {
            session.Expire();
        }

        if (idleSessions.Count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return idleSessions.Count;
    }
}
