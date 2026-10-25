using Microsoft.EntityFrameworkCore;
using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Application.Abstractions.Ports;
using Sentinel.Domain.Access;
using Sentinel.Domain.Common;
using Sentinel.Domain.Pages;

namespace Sentinel.Application.Pages.CreatePage;

internal sealed class CreatePageCommandHandler(IApplicationDbContext db) : ICommandHandler<CreatePageCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreatePageCommand request, CancellationToken cancellationToken)
    {
        Permission? permission = null;
        if (!string.IsNullOrWhiteSpace(request.RequiredPermission))
        {
            var parsed = Permission.Create(request.RequiredPermission);
            if (parsed.IsFailure)
                return parsed.Error!;

            // A page must require something concrete. "reports.*" on a page
            // would mean "any one of the reports permissions", which is a
            // different rule from the one the evaluator implements.
            if (parsed.Value.IsWildcard)
                return Error.Validation("Page.WildcardPermission", "A page must require a specific permission, not a wildcard.");

            permission = parsed.Value;
        }

        Guid? parentId = null;
        if (!string.IsNullOrWhiteSpace(request.ParentPath))
        {
            var parentPath = PagePath.TryNormalise(request.ParentPath);
            if (parentPath is null)
                return PageErrors.InvalidPath(request.ParentPath);

            var parent = await db.Pages.AsNoTracking().FirstOrDefaultAsync(p => p.Path == parentPath, cancellationToken);
            if (parent is null)
                return PageErrors.NotFound(parentPath);

            parentId = parent.Id;
        }

        var page = Page.Create(request.Path, request.Title, parentId, permission, request.IsPublic, request.SortOrder);
        if (page.IsFailure)
            return page.Error!;

        if (await db.Pages.AnyAsync(p => p.Path == page.Value.Path, cancellationToken))
            return PageErrors.DuplicatePath(page.Value.Path);

        db.Pages.Add(page.Value);
        await db.SaveChangesAsync(cancellationToken);
        return page.Value.Id;
    }
}
