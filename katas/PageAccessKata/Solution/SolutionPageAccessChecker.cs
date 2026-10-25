namespace PageAccessKata.Solution;

/// <summary>
/// A model answer, written the way you would write it in the test: one
/// public method, small private helpers, and the decisions stated up front.
/// </summary>
/// <remarks>
/// Decisions (each is a choice the brief left open; a reviewer wants to see
/// that you noticed it and chose on purpose):
/// <list type="bullet">
///   <item>Missing parent: the page is inaccessible. Failing closed is the safe default for access control.</item>
///   <item>Duplicate paths: the first definition wins and later ones are ignored. Throwing would also be defensible; ignoring keeps one bad row from hiding every page.</item>
///   <item>Null role lists are treated as empty. Role names are trimmed and compared ignoring case.</item>
///   <item>Parent cycles: every page on or below the cycle is inaccessible, even to admin (it can never be reached by navigation).</item>
///   <item>Admin bypasses role checks but not structure: an orphan is still inaccessible to admin.</item>
///   <item>Duplicate user names: the last one wins in the result dictionary. Names are assumed unique.</item>
/// </list>
/// Complexity: O(users × pages × depth) in the worst case; each page's
/// ancestry is resolved once per user thanks to memoisation.
/// </remarks>
public sealed class SolutionPageAccessChecker : IPageAccessChecker
{
    private const string AdminRole = "admin";

    public IReadOnlyDictionary<string, IReadOnlyList<string>> AccessiblePagesByUser(
        IEnumerable<KataUser> users,
        IEnumerable<KataPage> pages)
    {
        ArgumentNullException.ThrowIfNull(users);
        ArgumentNullException.ThrowIfNull(pages);

        var pagesByPath = IndexPages(pages);
        var result = new Dictionary<string, IReadOnlyList<string>>();

        foreach (var user in users)
        {
            var roles = new HashSet<string>(
                (user.Roles ?? []).Where(r => !string.IsNullOrWhiteSpace(r)).Select(r => r.Trim()),
                StringComparer.OrdinalIgnoreCase);

            var memo = new Dictionary<string, bool>();
            result[user.Name] = pagesByPath.Values
                .Where(page => CanOpen(page, roles, pagesByPath, memo, visiting: []))
                .Select(page => page.Path)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        return result;
    }

    private static Dictionary<string, KataPage> IndexPages(IEnumerable<KataPage> pages)
    {
        var index = new Dictionary<string, KataPage>(StringComparer.OrdinalIgnoreCase);
        foreach (var page in pages)
        {
            index.TryAdd(Normalise(page.Path), page); // first definition wins
        }
        return index;
    }

    private static bool CanOpen(
        KataPage page,
        HashSet<string> roles,
        Dictionary<string, KataPage> pagesByPath,
        Dictionary<string, bool> memo,
        HashSet<string> visiting)
    {
        var key = Normalise(page.Path);
        if (memo.TryGetValue(key, out var known))
            return known;

        // Seen on the way up: we are walking in a circle.
        if (!visiting.Add(key))
            return false;

        var allowed = ParentAllows(page, roles, pagesByPath, memo, visiting) && RolesAllow(page, roles);

        visiting.Remove(key);
        memo[key] = allowed;
        return allowed;
    }

    private static bool ParentAllows(
        KataPage page,
        HashSet<string> roles,
        Dictionary<string, KataPage> pagesByPath,
        Dictionary<string, bool> memo,
        HashSet<string> visiting)
    {
        if (string.IsNullOrWhiteSpace(page.ParentPath))
            return true;

        return pagesByPath.TryGetValue(Normalise(page.ParentPath), out var parent)
            && CanOpen(parent, roles, pagesByPath, memo, visiting);
    }

    private static bool RolesAllow(KataPage page, HashSet<string> roles)
    {
        var required = (page.RequiredRoles ?? []).Where(r => !string.IsNullOrWhiteSpace(r)).ToList();
        return required.Count == 0
            || roles.Contains(AdminRole)
            || required.Any(r => roles.Contains(r.Trim()));
    }

    /// <summary>"/Reports/" -> "/reports". The root "/" stays "/".</summary>
    private static string Normalise(string path)
    {
        var trimmed = path.Trim().ToLowerInvariant();
        return trimmed.Length > 1 ? trimmed.TrimEnd('/') : trimmed;
    }
}
