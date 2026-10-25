using Sentinel.Contracts;
using Sentinel.Domain.Access;
using Sentinel.Domain.Pages;

namespace Sentinel.Application.Pages;

/// <summary>Turns evaluator decisions into the shapes the three page-access slices return.</summary>
internal static class PageAccessMapping
{
    public static PageAccessDto ToDto(this PageAccessDecision decision, IReadOnlyDictionary<Guid, Page> pagesById)
    {
        var page = decision.Page;
        var parentPath = page.ParentId is { } parentId && pagesById.TryGetValue(parentId, out var parent)
            ? parent.Path
            : null;

        return new PageAccessDto(
            page.Path,
            page.Title,
            parentPath,
            page.RequiredPermission?.Value,
            decision.IsAllowed,
            decision.Reason.ToString());
    }

    /// <summary>
    /// Builds the menu from the allowed pages. A public page whose parent is
    /// hidden is promoted to the top level rather than dropped: the evaluator
    /// said the user may open it, so the menu must offer a way to get there.
    /// </summary>
    public static IReadOnlyList<NavigationNodeDto> ToNavigationTree(this IEnumerable<PageAccessDecision> decisions)
    {
        var allowed = decisions.Where(d => d.IsAllowed).Select(d => d.Page).ToList();
        var allowedIds = allowed.Select(p => p.Id).ToHashSet();

        var childrenByParent = allowed
            .Where(p => p.ParentId is { } id && allowedIds.Contains(id))
            .ToLookup(p => p.ParentId!.Value);

        var roots = allowed.Where(p => p.ParentId is not { } id || !allowedIds.Contains(id));

        return Build(roots, childrenByParent);
    }

    private static List<NavigationNodeDto> Build(IEnumerable<Page> pages, ILookup<Guid, Page> childrenByParent) =>
        pages
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Title, StringComparer.OrdinalIgnoreCase)
            .Select(p => new NavigationNodeDto(p.Path, p.Title, Build(childrenByParent[p.Id], childrenByParent)))
            .ToList();
}
