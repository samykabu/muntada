using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.Rooms.Domain.Template;
using Muntada.Rooms.Infrastructure;
using Muntada.SharedKernel.Domain.Exceptions;

namespace Muntada.Rooms.Application.Queries;

/// <summary>
/// Query to retrieve a single room template by ID.
/// </summary>
/// <param name="TenantId">The tenant to scope the query to.</param>
/// <param name="TemplateId">The template identifier.</param>
public sealed record GetRoomTemplateQuery(string TenantId, string TemplateId) : IRequest<RoomTemplate>;

/// <summary>
/// Handles <see cref="GetRoomTemplateQuery"/>.
/// </summary>
public sealed class GetRoomTemplateQueryHandler : IRequestHandler<GetRoomTemplateQuery, RoomTemplate>
{
    private readonly RoomsDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetRoomTemplateQueryHandler"/> class.
    /// </summary>
    public GetRoomTemplateQueryHandler(RoomsDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<RoomTemplate> Handle(GetRoomTemplateQuery request, CancellationToken cancellationToken)
    {
        var templateId = new RoomTemplateId(request.TemplateId);
        var template = await _db.RoomTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == templateId && t.TenantId == request.TenantId, cancellationToken);

        if (template is null)
            throw new EntityNotFoundException(nameof(RoomTemplate), request.TemplateId);

        return template;
    }
}
