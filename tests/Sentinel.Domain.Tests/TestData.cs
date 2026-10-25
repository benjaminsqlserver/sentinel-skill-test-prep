using Sentinel.Domain.Access;
using Sentinel.Domain.Pages;
using Sentinel.Domain.Security;

namespace Sentinel.Domain.Tests;

/// <summary>Small, readable builders. A test should say what is different about its data, and nothing else.</summary>
internal static class Places
{
    public static readonly GeoLocation Lagos = GeoLocation.Create("NG", "Lagos", 6.5244, 3.3792);
    public static readonly GeoLocation Ikeja = GeoLocation.Create("NG", "Ikeja", 6.6018, 3.3515); // ~9 km from Lagos
    public static readonly GeoLocation Abuja = GeoLocation.Create("NG", "Abuja", 9.0765, 7.3986);
    public static readonly GeoLocation London = GeoLocation.Create("GB", "London", 51.5072, -0.1276);
    public static readonly GeoLocation NewYork = GeoLocation.Create("US", "New York", 40.7128, -74.0060);
    public static readonly GeoLocation Frankfurt = GeoLocation.Create("DE", "Frankfurt", 50.1109, 8.6821);
}

internal static class Agents
{
    public const string Laptop = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36";
    public const string Phone = "Mozilla/5.0 (iPhone; CPU iPhone OS 18_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.6 Mobile/15E148 Safari/604.1";
}

/// <summary>Builds one user's sign-in history in time order, relative to a fixed "now".</summary>
internal sealed class History
{
    public static readonly DateTime Now = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

    private readonly List<SignInAttempt> _attempts = [];
    private readonly Guid _userId = Guid.NewGuid();

    public IReadOnlyList<SignInAttempt> Attempts => _attempts;

    public SignInAttempt Last => _attempts[^1];

    public History Success(DateTime atUtc, GeoLocation? where = null, string agent = Agents.Laptop) =>
        Add(atUtc, where ?? Places.Lagos, agent, succeeded: true);

    public History Failure(DateTime atUtc, GeoLocation? where = null, string agent = Agents.Laptop) =>
        Add(atUtc, where ?? Places.Lagos, agent, succeeded: false);

    /// <summary>A month of ordinary daily sign-ins from Lagos on the laptop, ending a week ago.</summary>
    public History WithOrdinaryBaseline(int days = 30)
    {
        for (var day = days + 7; day > 7; day--)
            Success(Now.AddDays(-day).AddHours(-3));
        return this;
    }

    public History Failures(DateTime startUtc, int count, TimeSpan spacing, GeoLocation? where = null)
    {
        for (var i = 0; i < count; i++)
            Failure(startUtc + spacing * i, where);
        return this;
    }

    private History Add(DateTime atUtc, GeoLocation where, string agent, bool succeeded)
    {
        _attempts.Add(SignInAttempt.Record(
            _userId, atUtc, "102.89.34.10", where, agent, succeeded,
            succeeded ? null : SignInFailureReason.WrongPassword).Value);
        return this;
    }
}

/// <summary>Builds a page tree with readable names instead of Guids.</summary>
internal sealed class PageTree
{
    private readonly Dictionary<string, Page> _byPath = [];

    public IReadOnlyList<Page> Pages => _byPath.Values.ToList();

    public Page this[string path] => _byPath[path];

    public PageTree Add(string path, string? parentPath = null, string? permission = null, bool isPublic = false)
    {
        var parentId = parentPath is null ? (Guid?)null : _byPath[parentPath].Id;
        var title = path == "/" ? "Home" : path.TrimStart('/');
        var page = Page.Create(path, title, parentId,
            permission is null ? null : Permission.From(permission), isPublic).Value;
        _byPath[page.Path] = page;
        return this;
    }

    /// <summary>The standard tree most tests share: / > reports > finance > export, plus public /help.</summary>
    public static PageTree Standard() => new PageTree()
        .Add("/")
        .Add("/reports", "/", "reports.view")
        .Add("/reports/finance", "/reports", "reports.finance")
        .Add("/reports/finance/export", "/reports/finance", "reports.finance.export")
        .Add("/reports/operations", "/reports", "reports.operations")
        .Add("/account", "/")
        .Add("/help", isPublic: true);
}

internal static class Subjects
{
    public static AccessSubject Active(params string[] granted) =>
        AccessSubject.Create(SubjectStatus.Active, granted.Select(Permission.From), []);

    public static AccessSubject ActiveWithDenies(string[] granted, string[] denied) =>
        AccessSubject.Create(SubjectStatus.Active, granted.Select(Permission.From), denied.Select(Permission.From));

    public static AccessSubject Suspended(params string[] granted) =>
        AccessSubject.Create(SubjectStatus.Suspended, granted.Select(Permission.From), []);
}
