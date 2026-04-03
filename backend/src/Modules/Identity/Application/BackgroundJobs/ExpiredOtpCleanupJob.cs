using Microsoft.EntityFrameworkCore;
using Muntada.Identity.Domain.Otp;
using Muntada.Identity.Infrastructure;

namespace Muntada.Identity.Application.BackgroundJobs;

/// <summary>
/// Background job that finds expired OTP challenges and marks them as expired.
/// Should be executed on a recurring schedule to clean up stale challenges.
/// </summary>
public sealed class ExpiredOtpCleanupJob
{
    private readonly IdentityDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of <see cref="ExpiredOtpCleanupJob"/>.
    /// </summary>
    /// <param name="dbContext">The Identity module database context.</param>
    public ExpiredOtpCleanupJob(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Executes the cleanup by finding all pending OTP challenges that have passed
    /// their expiration time and transitioning them to the expired status.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of challenges that were marked as expired.</returns>
    public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var expiredChallenges = await _dbContext.Set<OtpChallenge>()
            .Where(c => c.Status == OtpStatus.Pending && c.ExpiresAt < now)
            .ToListAsync(cancellationToken);

        foreach (var challenge in expiredChallenges)
        {
            challenge.IncrementFailedAttempts(); // Forces transition to Expired when at max
        }

        if (expiredChallenges.Count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return expiredChallenges.Count;
    }
}
