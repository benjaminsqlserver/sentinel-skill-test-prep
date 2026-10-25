using Microsoft.EntityFrameworkCore;
using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Application.Abstractions.Ports;
using Sentinel.Application.Access;
using Sentinel.Contracts;
using Sentinel.Domain.Access;
using Sentinel.Domain.Common;
using Sentinel.Domain.Users;

namespace Sentinel.Application.Pages.GetPageAccess;

internal sealed class GetPageAccessQueryHandler(IApplicationDbContext db) : IQueryHandler<GetPageAccessQuery, IReadOnlyList<PageAccessDto>>
{
    public async Task<Result<IReadOnlyList<PageAccessDto>>> Handle(GetPageAccessQuery request, CancellationToken cancellationToken)
    {
        var subject = await db.LoadAccessSubjectAsync(request.UserId, cancellationToken);
        if (subject is null)
            return UserErrors.NotFound(request.UserId);

        var pages = await db.Pages.AsNoTracking().OrderBy(p => p.Path).ToListAsync(cancellationToken);
        var pagesById = pages.ToDictionary(p => p.Id);

        IReadOnlyList<PageAccessDto> result = PageAccessEvaluator
            .EvaluateAll(subject, pages)
            .Select(d => d.ToDto(pagesById))
            .ToList();

        return Result.Success(result);
    }
}
