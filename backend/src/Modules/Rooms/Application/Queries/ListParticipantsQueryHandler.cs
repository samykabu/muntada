using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.Rooms.Domain.Occurrence;
using Muntada.Rooms.Domain.Participant;
using Muntada.Rooms.Infrastructure;
using Muntada.Rooms.Infrastructure.Cache;
using Muntada.SharedKernel.Domain.Exceptions;

namespace Muntada.Rooms.Application.Queries;

/// <summary>
/// Query to list participants for a room occurrence.
/// For Live rooms, reads from Redis cache for real-time data.
/// For Ended/Archived rooms, falls back to SQL.
/// </summary>
/// <param name="OccurrenceId">The room occurrence to list participants for.</param>
/// <param name="TenantId">The owning tenant's identifier.</param>
public sealed record ListParticipantsQuery(
    string OccurrenceId,
    string TenantId) : IRequest<List<ParticipantInfo>>;

/// <summary>
/// Represents a unified participant info record, sourced from either cache or SQL.
/// </summary>
/// <param name="ParticipantId">The participant state identifier.</param>
/// <param name="UserId">The user ID, or null for guests.</param>
/// <param name="DisplayName">The participant's display name.</param>
/// <param name="Role">The participant's role.</param>
/// <param name="AudioState">Current audio state.</param>
/// <param name="VideoState">Current video state.</param>
/// <param name="JoinedAt">UTC timestamp when joined.</param>
/// <param name="LeftAt">UTC timestamp when left, or null if still in room.</param>
public sealed record ParticipantInfo(
    string ParticipantId,
    string? UserId,
    string DisplayName,
    ParticipantRole Role,
    MediaState AudioState,
    MediaState VideoState,
    DateTimeOffset JoinedAt,
    DateTimeOffset? LeftAt);

/// <summary>
/// Handles <see cref="ListParticipantsQuery"/> — reads participants from Redis for live rooms,
/// or from SQL for ended rooms.
/// </summary>
public sealed class ListParticipantsQueryHandler : IRequestHandler<ListParticipantsQuery, List<ParticipantInfo>>
{
    private readonly RoomsDbContext _db;
    private readonly ParticipantStateCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListParticipantsQueryHandler"/> class.
    /// </summary>
    public ListParticipantsQueryHandler(RoomsDbContext db, ParticipantStateCache cache)
    {
        _db = db;
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<List<ParticipantInfo>> Handle(ListParticipantsQuery request, CancellationToken cancellationToken)
    {
        var occurrenceId = new RoomOccurrenceId(request.OccurrenceId);
        var occurrence = await _db.RoomOccurrences
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == occurrenceId && o.TenantId == request.TenantId, cancellationToken);

        if (occurrence is null)
            throw new EntityNotFoundException(nameof(RoomOccurrence), request.OccurrenceId);

        // For live rooms, read from Redis cache for real-time data
        if (occurrence.Status == RoomOccurrenceStatus.Live || occurrence.Status == RoomOccurrenceStatus.Grace)
        {
            var cached = await _cache.GetParticipantsAsync(request.OccurrenceId);
            return cached.Select(c => new ParticipantInfo(
                c.ParticipantId,
                c.UserId,
                c.DisplayName,
                c.Role,
                c.AudioState,
                c.VideoState,
                c.JoinedAt,
                null)).ToList();
        }

        // For ended/archived rooms, fall back to SQL
        var participants = await _db.RoomParticipantStates
            .AsNoTracking()
            .Where(p => p.RoomOccurrenceId == occurrenceId)
            .OrderBy(p => p.JoinedAt)
            .ToListAsync(cancellationToken);

        return participants.Select(p => new ParticipantInfo(
            p.Id.Value,
            p.UserId,
            p.DisplayName,
            p.Role,
            p.AudioState,
            p.VideoState,
            p.JoinedAt,
            p.LeftAt)).ToList();
    }
}
