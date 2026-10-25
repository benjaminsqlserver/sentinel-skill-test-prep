using Sentinel.Domain.Access;
using Sentinel.Domain.Common;

namespace Sentinel.Domain.Pages;

/// <summary>
/// A screen in the portal. Pages form a tree through <see cref="ParentId"/>;
/// the tree is the navigation menu, and it is also the access model, because
/// a child page is only reachable through its parent.
/// </summary>
public sealed class Page : AggregateRoot
{
    public const int MaxTitleLength = 100;

    private Page(Guid id, string path, string title, Guid? parentId, Permission? requiredPermission, bool isPublic, int sortOrder)
        : base(id)
    {
        Path = path;
        Title = title;
        ParentId = parentId;
        RequiredPermission = requiredPermission;
        IsPublic = isPublic;
        SortOrder = sortOrder;
    }

    // For EF Core.
    private Page() { }

    /// <summary>Always normalised. See <see cref="PagePath"/>.</summary>
    public string Path { get; private set; } = null!;

    public string Title { get; private set; } = null!;

    public Guid? ParentId { get; private set; }

    /// <summary>Null means "any signed-in user"; it does not mean "anyone".</summary>
    public Permission? RequiredPermission { get; private set; }

    /// <summary>Visible without signing in. A public page cannot also require a permission.</summary>
    public bool IsPublic { get; private set; }

    public int SortOrder { get; private set; }

    public static Result<Page> Create(
        string path,
        string title,
        Guid? parentId = null,
        Permission? requiredPermission = null,
        bool isPublic = false,
        int sortOrder = 0,
        Guid? id = null)
    {
        var normalisedPath = PagePath.TryNormalise(path);
        if (normalisedPath is null)
            return PageErrors.InvalidPath(path);

        if (string.IsNullOrWhiteSpace(title))
            return PageErrors.TitleRequired;

        if (title.Trim().Length > MaxTitleLength)
            return PageErrors.TitleTooLong;

        if (isPublic && requiredPermission is not null)
            return PageErrors.PublicPageWithPermission;

        var pageId = id ?? Guid.CreateVersion7();
        if (parentId == pageId)
            return PageErrors.OwnParent;

        return new Page(pageId, normalisedPath, title.Trim(), parentId, requiredPermission, isPublic, sortOrder);
    }

    /// <summary>
    /// Re-parents the page. Only the trivial cycle (a page under itself) is
    /// caught here; longer cycles need the whole tree, so the evaluator
    /// treats them as corrupt data rather than trusting this method alone.
    /// </summary>
    public Result MoveUnder(Guid? parentId)
    {
        if (parentId == Id)
            return PageErrors.OwnParent;

        ParentId = parentId;
        return Result.Success();
    }

    public Result RequirePermission(Permission? permission)
    {
        if (IsPublic && permission is not null)
            return PageErrors.PublicPageWithPermission;

        RequiredPermission = permission;
        return Result.Success();
    }
}

public static class PageErrors
{
    public static Error InvalidPath(string? path) =>
        Error.Validation("Page.InvalidPath", $"'{path}' is not a valid page path. Paths look like '/reports/finance'.");

    public static readonly Error TitleRequired =
        Error.Validation("Page.TitleRequired", "A page title is required.");

    public static readonly Error TitleTooLong =
        Error.Validation("Page.TitleTooLong", $"A page title cannot exceed {Page.MaxTitleLength} characters.");

    public static readonly Error PublicPageWithPermission =
        Error.Validation("Page.PublicPageWithPermission", "A public page cannot require a permission.");

    public static readonly Error OwnParent =
        Error.Validation("Page.OwnParent", "A page cannot be its own parent.");

    public static Error DuplicatePath(string path) =>
        Error.Conflict("Page.DuplicatePath", $"A page already exists at '{path}'.");

    public static Error ParentNotFound(Guid parentId) =>
        Error.NotFound("Page.ParentNotFound", $"Parent page {parentId} does not exist.");

    public static Error NotFound(string path) =>
        Error.NotFound("Page.NotFound", $"No page exists at '{path}'.");
}
