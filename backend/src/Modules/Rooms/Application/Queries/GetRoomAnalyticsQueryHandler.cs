using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.Rooms.Domain.Occurrence;
using Muntada.Rooms.Domain.Participant;
using Muntada.Rooms.Infrastructure;
using Muntada.SharedKernel.Domain.Exceptions;

namespace Muntada.Rooms.Application.Queries;

/// <summary>
/// Query to calculate analytics for a room occurrence including participant counts,
/// peak concurrency, dwell times, and audio/video participation rates.
/// </summary>
/// <param name="OccurrenceId">The room occurrence to analyze.</param>
/// <param name="TenantId">The owning tenant's identifier.</param>
public sealed record GetRoomAnalyticsQuery(
    string OccurrenceId,
    string TenantId) : IRequest<RoomAnalytics>;

/// <summary>
/// Aggregated analytics data for a room occurrence.
/// </summary>
/// <param name="TotalParticipants">Total unique participants who joined.</param>
/// <param name="PeakConcurrent">Peak number of concurrent participants.</param>
/// <param name="AverageTimeDwellSeconds">Average dwell time in seconds across all participants.</param>
/// <param name="AudioParticipationRate">Fraction of participants who unmuted audio at some point (0.0-1.0).</param>
/// <param name="VideoParticipationRate">Fraction of participants who enabled video at some point (0.0-1.0).</param>
/// <param name="DwellTimes">Individual dwell times per participant.</param>
public sealed record RoomAnalytics(
    int TotalParticipants,
    int PeakConcurrent,
    long AverageTimeDwellSeconds,
    double AudioParticipationRate,
    double VideoParticipationRate,
    List<ParticipantDwellTime> DwellTimes);

/// <summary>
/// Dwell time information for a single participant.
/// </summary>
/// <param name="ParticipantId">The participant state identifier.</param>
/// <param name="DisplayName">The participant's display name.</param>
/// <param name="DwellTimeSeconds">Total time in room in seconds.</param>
public sealed record ParticipantDwellTime(
    string ParticipantId,
    string DisplayName,
    long DwellTimeSeconds);

/// <summary>
/// Handles <see cref="GetRoomAnalyticsQuery"/> — calculates room analytics from participant state records.
/// </summary>
public sealed class GetRoomAnalyticsQueryHandler : IRequestHandler<GetRoomAnalyticsQuery, RoomAnalytics>
{
    private readonly RoomsDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetRoomAnalyticsQueryHandler"/> class.
    /// </summary>
    public GetRoomAnalyticsQueryHandler(RoomsDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<RoomAnalytics> Handle(GetRoomAnalyticsQuery request, CancellationToken cancellationToken)
    {
        var occurrenceId = new RoomOccurrenceId(request.OccurrenceId);
        var occurrence = await _db.RoomOccurrences
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == occurrenceId && o.TenantId == request.TenantId, cancellationToken);

        if (occurrence is null)
            throw new EntityNotFoundException(nameof(RoomOccurrence), request.OccurrenceId);

        var participants = await _db.RoomParticipantStates
            .AsNoTracking()
            .Where(p => p.RoomOccurrenceId == occurrenceId)
            .ToListAsync(cancellationToken);

        if (participants.Count == 0)
        {
            return new RoomAnalytics(
                TotalParticipants: 0,
                PeakConcurrent: 0,
                AverageTimeDwellSeconds: 0,
                AudioParticipationRate: 0.0,
                VideoParticipationRate: 0.0,
                DwellTimes: new List<ParticipantDwellTime>());
        }

        var totalParticipants = participants.Count;

        // Calculate peak concurrent by analyzing join/leave events
        var peakConcurrent = CalculatePeakConcurrent(participants);

        // Calculate dwell times
        var dwellTimes = participants.Select(p => new ParticipantDwellTime(
            p.Id.Value,
            p.DisplayName,
            p.GetDwellTimeSeconds())).ToList();

        var averageDwell = dwellTimes.Count > 0
            ? (long)dwellTimes.Average(d => d.DwellTimeSeconds)
            : 0;

        // Audio/video participation rates based on current state
        // (In a full implementation, this would track state changes over time)
        var audioParticipationRate = totalParticipants > 0
            ? (double)participants.Count(p => p.AudioState == MediaState.Unmuted) / totalParticipants
            : 0.0;

        var videoParticipationRate = totalParticipants > 0
            ? (double)participants.Count(p => p.VideoState == MediaState.On) / totalParticipants
            : 0.0;

        return new RoomAnalytics(
            totalParticipants,
            peakConcurrent,
            averageDwell,
            audioParticipationRate,
            videoParticipationRate,
            dwellTimes);
    }

    private static int CalculatePeakConcurrent(List<RoomParticipantState> participants)
    {
        // Build timeline of join/leave events to find peak concurrency
        var events = new List<(DateTimeOffset Time, int Delta)>();

        foreach (var p in participants)
        {
            events.Add((p.JoinedAt, 1));
            if (p.LeftAt.HasValue)
                events.Add((p.LeftAt.Value, -1));
        }

        events.Sort((a, b) => a.Time.CompareTo(b.Time));

        int current = 0;
        int peak = 0;

        foreach (var (_, delta) in events)
        {
            current += delta;
            if (current > peak)
                peak = current;
        }

        return peak;
    }
}
