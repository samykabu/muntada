using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.Rooms.Domain.Series;
using Muntada.Rooms.Infrastructure;
using Muntada.SharedKernel.Domain.Exceptions;

namespace Muntada.Rooms.Application.Commands;

/// <summary>
/// Command to end a room series, preventing further occurrence generation.
/// </summary>
/// <param name="TenantId">The owning tenant's identifier.</param>
/// <param name="SeriesId">The series to end.</param>
public sealed record EndRoomSeriesCommand(
    string TenantId,
    string SeriesId) : IRequest<RoomSeries>;

/// <summary>
/// Handles <see cref="EndRoomSeriesCommand"/> — ends the series and marks it as inactive.
/// </summary>
public sealed class EndRoomSeriesCommandHandler : IRequestHandler<EndRoomSeriesCommand, RoomSeries>
{
    private readonly RoomsDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="EndRoomSeriesCommandHandler"/> class.
    /// </summary>
    public EndRoomSeriesCommandHandler(RoomsDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<RoomSeries> Handle(EndRoomSeriesCommand request, CancellationToken cancellationToken)
    {
        var seriesId = new RoomSeriesId(request.SeriesId);
        var series = await _db.RoomSeries
            .FirstOrDefaultAsync(s => s.Id == seriesId && s.TenantId == request.TenantId, cancellationToken);

        if (series is null)
            throw new EntityNotFoundException(nameof(RoomSeries), request.SeriesId);

        series.End();
        series.IncrementVersion();

        await _db.SaveChangesAsync(cancellationToken);

        return series;
    }
}
