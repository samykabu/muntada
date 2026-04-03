using System.Security.Cryptography;
using System.Text;
using MediatR;
using Muntada.Identity.Domain.GuestLink;
using Muntada.Identity.Infrastructure;

namespace Muntada.Identity.Application.Commands;

/// <summary>
/// Command to generate a guest magic link for anonymous room access.
/// </summary>
/// <param name="RoomOccurrenceId">The room occurrence this link grants access to.</param>
/// <param name="CreatedByUserId">The user creating the magic link.</param>
public sealed record GenerateGuestMagicLinkCommand(Guid RoomOccurrenceId, Guid CreatedByUserId)
    : IRequest<GenerateGuestMagicLinkResult>;

/// <summary>
/// Result returned after a guest magic link is created.
/// </summary>
/// <param name="LinkId">The unique identifier of the created magic link.</param>
/// <param name="Token">The plaintext token to include in the magic link URL (returned only once).</param>
public sealed record GenerateGuestMagicLinkResult(Guid LinkId, string Token);

/// <summary>
/// Handles <see cref="GenerateGuestMagicLinkCommand"/> by generating a cryptographically secure
/// token, hashing it with SHA-256, creating a <see cref="GuestMagicLink"/>, and returning
/// the plaintext token for inclusion in the link URL.
/// </summary>
public sealed class GenerateGuestMagicLinkCommandHandler : IRequestHandler<GenerateGuestMagicLinkCommand, GenerateGuestMagicLinkResult>
{
    private static readonly TimeSpan LinkExpiry = TimeSpan.FromDays(7);

    private readonly IdentityDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of <see cref="GenerateGuestMagicLinkCommandHandler"/>.
    /// </summary>
    /// <param name="dbContext">The Identity module database context.</param>
    public GenerateGuestMagicLinkCommandHandler(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Handles magic link generation: creates a 32-byte random token, hashes it with SHA-256,
    /// persists the <see cref="GuestMagicLink"/> entity, and returns the plaintext token.
    /// </summary>
    /// <param name="request">The generate guest magic link command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The link ID and plaintext token for URL construction.</returns>
    public async Task<GenerateGuestMagicLinkResult> Handle(GenerateGuestMagicLinkCommand request, CancellationToken cancellationToken)
    {
        // Generate 32-byte random token
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

        // Hash with SHA-256 for storage
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

        // Create magic link entity
        var link = GuestMagicLink.Create(
            request.RoomOccurrenceId,
            request.CreatedByUserId,
            tokenHash,
            LinkExpiry);

        _dbContext.Set<GuestMagicLink>().Add(link);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new GenerateGuestMagicLinkResult(link.Id, rawToken);
    }
}
