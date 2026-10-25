using System.Text.RegularExpressions;

namespace Sentinel.Domain.Pages;

/// <summary>
/// The canonical form of a page route: leading slash, lower case, no
/// trailing slash (except the root itself), no empty segments.
/// <c>" /Admin/Users/ "</c> becomes <c>"/admin/users"</c>.
/// </summary>
public static partial class PagePath
{
    public const string Root = "/";

    /// <summary>The normalised path, or null when <paramref name="raw"/> cannot be a page path.</summary>
    public static string? TryNormalise(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var path = raw.Trim().ToLowerInvariant();

        if (!path.StartsWith('/'))
            return null;

        if (path.Length > 1)
            path = path.TrimEnd('/');

        if (path.Length == 0)
            return Root; // the input was "///"

        return path == Root || SegmentedPath().IsMatch(path) ? path : null;
    }

    /// <summary>"/admin/users" -> "/admin"; "/admin" -> "/"; "/" -> null.</summary>
    public static string? ParentOf(string normalisedPath)
    {
        if (normalisedPath == Root)
            return null;

        var lastSlash = normalisedPath.LastIndexOf('/');
        return lastSlash == 0 ? Root : normalisedPath[..lastSlash];
    }

    // One or more "/segment" where a segment is [a-z0-9-]+ and has no empty parts.
    [GeneratedRegex(@"^(/[a-z0-9][a-z0-9-]*)+$")]
    private static partial Regex SegmentedPath();
}
