using Microsoft.EntityFrameworkCore;
using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Application.Abstractions.Ports;
using Sentinel.Application.Access;
using Sentinel.Contracts;
using Sentinel.Domain.Access;
using Sentinel.Domain.Common;

namespace Sentinel.Application.Pages.GetNavigation;

internal sealed class GetNavigationQueryHandler(IApplicationDbContext db) : IQueryHandler<GetNavigationQuery, IReadOnlyList<NavigationNodeDto>>
{
    public async Task<Result<IReadOnlyList<NavigationNodeDto>>> Handle(GetNavigationQuery request, CancellationToken cancellationToken)
    {
        // An id that names no user (deleted since the token was issued) is
        // treated as anonymous rather than as an error: the menu should
        // degrade to the public pages, not fail to render.
        var subject = await db.LoadAccessSubjectAsync(request.UserId, cancellationToken) ?? AccessSubject.Anonymous;

        var pages = await db.Pages.AsNoTracking().ToListAsync(cancellationToken);

        return Result.Success(PageAccessEvaluator.EvaluateAll(subject, pages).ToNavigationTree());
    }
}
