using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.Identity.Domain.GuestLink;
using Muntada.Identity.Infrastructure;

namespace Muntada.Identity.Application.Commands;

/// <summary>
/// Command to revoke a guest magic link, preventing further use.
/// </summary>
/// <param name="LinkId">The unique identifier of the magic link to revoke.</param>
/// <param name="UserId">The unique identifier of the user requesting revocation (must be the link creator).</param>
public sealed record RevokeMagicLinkCommand(Guid LinkId, Guid UserId) : IRequest<bool>;

/// <summary>
/// Handles <see cref="RevokeMagicLinkCommand"/> by finding the magic link,
/// verifying ownership, and revoking it.
/// </summary>
public sealed class RevokeMagicLinkCommandHandler : IRequestHandler<RevokeMagicLinkCommand, bool>
{
    private readonly IdentityDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of <see cref="RevokeMagicLinkCommandHandler"/>.
    /// </summary>
    /// <param name="dbContext">The Identity module database context.</param>
    public RevokeMagicLinkCommandHandler(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Handles magic link revocation: finds the link by ID, verifies the requesting user
    /// is the creator, and calls <see cref="GuestMagicLink.Revoke"/>.
    /// </summary>
    /// <param name="request">The revoke magic link command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the link was revoked; <c>false</c> if not found or not owned.</returns>
    public async Task<bool> Handle(RevokeMagicLinkCommand request, CancellationToken cancellationToken)
    {
        var link = await _dbContext.Set<GuestMagicLink>()
            .FirstOrDefaultAsync(l => l.Id == request.LinkId && l.CreatedByUserId == request.UserId, cancellationToken);

        if (link is null || link.Status != GuestMagicLinkStatus.Active)
            return false;

        link.Revoke();
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
