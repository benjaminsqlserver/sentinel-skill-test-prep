using Sentinel.Domain.Pages;

namespace Sentinel.Domain.Access;

/// <summary>
/// Decides which pages a subject may open. The rules, in the order they apply:
/// <list type="number">
///   <item>A page that is part of a parent cycle is denied (the data is corrupt; fail closed).</item>
///   <item>A public page is allowed for everyone, signed in or not.</item>
///   <item>Anonymous and suspended subjects are denied everything else.</item>
///   <item>A page whose parent is missing is denied (fail closed).</item>
///   <item>A page whose parent is denied is denied: you cannot reach a room through a locked door.</item>
///   <item>A page with no required permission is allowed for any active user.</item>
///   <item>An explicit deny covering the required permission wins.</item>
///   <item>Otherwise the page is allowed if any grant covers the required permission.</item>
/// </list>
/// </summary>
/// <remarks>
/// The evaluator is a pure function of its inputs. It never throws on bad
/// data from the database (orphans and cycles become denials with a reason)
/// because a navigation menu that crashes is worse than one that hides a page.
/// </remarks>
public sealed class PageAccessEvaluator
{
    private readonly AccessSubject _subject;
    private readonly Dictionary<Guid, Page> _pagesById = [];
    private readonly Dictionary<Guid, PageAccessDecision> _decisions = [];

    private PageAccessEvaluator(AccessSubject subject, IEnumerable<Page> pages)
    {
        _subject = subject;
        foreach (var page in pages)
        {
            if (!_pagesById.TryAdd(page.Id, page))
                throw new ArgumentException($"Page id {page.Id} appears more than once.", nameof(pages));
        }
    }

    /// <summary>One decision per page, in the order the pages were supplied.</summary>
    public static IReadOnlyList<PageAccessDecision> EvaluateAll(AccessSubject subject, IEnumerable<Page> pages)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(pages);

        var pageList = pages.ToList();
        var evaluator = new PageAccessEvaluator(subject, pageList);
        return pageList.Select(evaluator.Decide).ToList();
    }

    /// <summary>
    /// The decision for the page at <paramref name="path"/>, or null when no
    /// such page exists. Paths are compared after normalisation, so
    /// <c>/Admin/Users/</c> and <c>/admin/users</c> are the same page.
    /// </summary>
    public static PageAccessDecision? EvaluatePath(AccessSubject subject, IEnumerable<Page> pages, string path)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(pages);

        var normalised = PagePath.TryNormalise(path);
        if (normalised is null)
            return null;

        var pageList = pages.ToList();
        var target = pageList.FirstOrDefault(p => p.Path == normalised);
        return target is null ? null : new PageAccessEvaluator(subject, pageList).Decide(target);
    }

    private PageAccessDecision Decide(Page page)
    {
        if (_decisions.TryGetValue(page.Id, out var known))
            return known;

        var decision = DecideUncached(page);
        _decisions[page.Id] = decision;
        return decision;
    }

    private PageAccessDecision DecideUncached(Page page)
    {
        if (IsOnCycle(page))
            return Deny(page, AccessReason.CyclicHierarchy);

        if (page.IsPublic)
            return Allow(page, AccessReason.PublicPage);

        switch (_subject.Status)
        {
            case SubjectStatus.Anonymous: return Deny(page, AccessReason.NotAuthenticated);
            case SubjectStatus.Suspended: return Deny(page, AccessReason.UserSuspended);
        }

        if (page.ParentId is { } parentId)
        {
            if (!_pagesById.TryGetValue(parentId, out var parent))
                return Deny(page, AccessReason.OrphanedPage);

            // Safe to recurse: this page is not on a cycle, so walking up
            // either reaches a root or reaches a page that *is* on a cycle,
            // which returns above without recursing further.
            if (!Decide(parent).IsAllowed)
                return Deny(page, AccessReason.ParentDenied);
        }

        if (page.RequiredPermission is not { } required)
            return Allow(page, AccessReason.AuthenticatedUser);

        if (_subject.IsDenied(required))
            return Deny(page, AccessReason.ExplicitlyDenied);

        return _subject.IsGranted(required)
            ? Allow(page, AccessReason.PermissionGranted)
            : Deny(page, AccessReason.MissingPermission);
    }

    /// <summary>True when following parents from <paramref name="page"/> leads back to it.</summary>
    private bool IsOnCycle(Page page)
    {
        var seen = new HashSet<Guid>();
        var current = page;

        while (current.ParentId is { } parentId && _pagesById.TryGetValue(parentId, out var parent))
        {
            if (parent.Id == page.Id)
                return true;

            // A cycle further up that does not include this page: the page
            // hangs below the cycle rather than sitting on it. ParentDenied
            // will deal with it when its ancestors are decided.
            if (!seen.Add(parent.Id))
                return false;

            current = parent;
        }

        return false;
    }

    private static PageAccessDecision Allow(Page page, AccessReason reason) => new(page, true, reason);
    private static PageAccessDecision Deny(Page page, AccessReason reason) => new(page, false, reason);
}
