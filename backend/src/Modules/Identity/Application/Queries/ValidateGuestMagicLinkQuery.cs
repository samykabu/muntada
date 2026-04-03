using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.Identity.Domain.GuestLink;
using Muntada.Identity.Infrastructure;

namespace Muntada.Identity.Application.Queries;

/// <summary>
/// Query to validate a guest magic link token and create a guest session.
/// </summary>
/// <param name="Token">The plaintext magic link token from the URL.</param>
/// <param name="UserAgent">The User-Agent header from the client request.</param>
/// <param name="IpAddress">The IP address of the client.</param>
public sealed record ValidateGuestMagicLinkQuery(string Token, string UserAgent, string IpAddress)
    : IRequest<GuestSessionResult>;

/// <summary>
/// Result returned after successful magic link validation.
/// </summary>
/// <param name="LinkId">The unique identifier of the validated magic link.</param>
/// <param name="RoomOccurrenceId">The room occurrence the guest is granted access to.</param>
/// <param name="GuestSessionId">A generated guest session identifier.</param>
public sealed record GuestSessionResult(Guid LinkId, Guid RoomOccurrenceId, Guid GuestSessionId);

/// <summary>
/// Handles <see cref="ValidateGuestMagicLinkQuery"/> by hashing the token,
/// looking up the magic link, validating it, incrementing usage, and creating a guest session.
/// </summary>
public sealed class ValidateGuestMagicLinkQueryHandler : IRequestHandler<ValidateGuestMagicLinkQuery, GuestSessionResult>
{
    private readonly IdentityDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of <see cref="ValidateGuestMagicLinkQueryHandler"/>.
    /// </summary>
    /// <param name="dbContext">The Identity module database context.</param>
    public ValidateGuestMagicLinkQueryHandler(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Handles magic link validation: hashes the incoming token with SHA-256,
    /// finds the matching link, validates it, increments usage count,
    /// and returns a guest session result.
    /// </summary>
    /// <param name="request">The validate guest magic link query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A guest session result with room access details.</returns>
    /// <exception cref="FluentValidation.ValidationException">
    /// Thrown with a generic message when the magic link is invalid or expired (FR-018).
    /// </exception>
    public async Task<GuestSessionResult> Handle(ValidateGuestMagicLinkQuery request, CancellationToken cancellationToken)
    {
        // Hash incoming token
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Token)));

        var link = await _dbContext.Set<GuestMagicLink>()
            .FirstOrDefaultAsync(l => l.TokenHash == tokenHash, cancellationToken);

        if (link is null || !link.IsValid())
        {
            throw new FluentValidation.ValidationException(
                [new FluentValidation.Results.ValidationFailure("", "Invalid or expired link.")]);
        }

        // Record usage
        link.IncrementUsage();

        // Generate a guest session ID
        var guestSessionId = Guid.NewGuid();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new GuestSessionResult(link.Id, link.RoomOccurrenceId, guestSessionId);
    }
}
