using Microsoft.EntityFrameworkCore;
using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Application.Abstractions.Ports;
using Sentinel.Contracts;
using Sentinel.Domain.Common;

namespace Sentinel.Application.Pages.GetPages;

internal sealed class GetPagesQueryHandler(IApplicationDbContext db) : IQueryHandler<GetPagesQuery, IReadOnlyList<PageDto>>
{
    public async Task<Result<IReadOnlyList<PageDto>>> Handle(GetPagesQuery request, CancellationToken cancellationToken)
    {
        var pages = await db.Pages.AsNoTracking().OrderBy(p => p.Path).ToListAsync(cancellationToken);

        IReadOnlyList<PageDto> result = pages
            .Select(p => new PageDto(p.Id, p.Path, p.Title, p.ParentId, p.RequiredPermission?.Value, p.IsPublic, p.SortOrder))
            .ToList();

        return Result.Success(result);
    }
}
