using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.Rooms.Domain.Invite;
using Muntada.Rooms.Infrastructure;
using Muntada.SharedKernel.Domain.Exceptions;

namespace Muntada.Rooms.Application.Commands;

/// <summary>
/// Command to revoke a pending room invite, invalidating its token immediately.
/// </summary>
/// <param name="InviteId">The invite to revoke.</param>
/// <param name="TenantId">The owning tenant's identifier for authorization.</param>
public sealed record RevokeRoomInviteCommand(
    string InviteId,
    string TenantId) : IRequest<Unit>;

/// <summary>
/// Handles <see cref="RevokeRoomInviteCommand"/> — revokes a pending invite.
/// </summary>
public sealed class RevokeRoomInviteCommandHandler : IRequestHandler<RevokeRoomInviteCommand, Unit>
{
    private readonly RoomsDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="RevokeRoomInviteCommandHandler"/> class.
    /// </summary>
    public RevokeRoomInviteCommandHandler(RoomsDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<Unit> Handle(RevokeRoomInviteCommand request, CancellationToken cancellationToken)
    {
        var inviteId = new RoomInviteId(request.InviteId);

        // Join to occurrence to verify tenant ownership
        var invite = await _db.RoomInvites
            .Join(
                _db.RoomOccurrences.Where(o => o.TenantId == request.TenantId),
                i => i.RoomOccurrenceId,
                o => o.Id,
                (i, _) => i)
            .FirstOrDefaultAsync(i => i.Id == inviteId, cancellationToken);

        if (invite is null)
            throw new EntityNotFoundException(nameof(RoomInvite), request.InviteId);

        invite.Revoke();
        await _db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
