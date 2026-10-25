namespace PageAccessKata;

public sealed record KataUser(string Name, IReadOnlyList<string>? Roles);

public sealed record KataPage(string Path, string? ParentPath, IReadOnlyList<string>? RequiredRoles);

public interface IPageAccessChecker
{
    /// <summary>User name -> accessible page paths, sorted alphabetically.</summary>
    IReadOnlyDictionary<string, IReadOnlyList<string>> AccessiblePagesByUser(
        IEnumerable<KataUser> users,
        IEnumerable<KataPage> pages);
}
