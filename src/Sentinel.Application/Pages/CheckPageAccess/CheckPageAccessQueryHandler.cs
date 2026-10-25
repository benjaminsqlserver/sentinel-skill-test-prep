using Microsoft.EntityFrameworkCore;
using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Application.Abstractions.Ports;
using Sentinel.Application.Access;
using Sentinel.Contracts;
using Sentinel.Domain.Access;
using Sentinel.Domain.Common;
using Sentinel.Domain.Pages;

namespace Sentinel.Application.Pages.CheckPageAccess;

internal sealed class CheckPageAccessQueryHandler(IApplicationDbContext db) : IQueryHandler<CheckPageAccessQuery, PageAccessDto>
{
    public async Task<Result<PageAccessDto>> Handle(CheckPageAccessQuery request, CancellationToken cancellationToken)
    {
        var subject = await db.LoadAccessSubjectAsync(request.UserId, cancellationToken) ?? AccessSubject.Anonymous;

        // The whole tree is needed even for one page: the answer depends on
        // every ancestor. Sentinel has tens of pages, not thousands; Chapter 10
        // discusses what to do when that stops being true.
        var pages = await db.Pages.AsNoTracking().ToListAsync(cancellationToken);

        var decision = PageAccessEvaluator.EvaluatePath(subject, pages, request.Path);
        if (decision is null)
            return PageErrors.NotFound(request.Path);

        return decision.ToDto(pages.ToDictionary(p => p.Id));
    }
}
