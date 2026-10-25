using PageAccessKata.Solution;
using PageAccessKata.Starter;

namespace PageAccessKata.Tests;

/// <summary>
/// One set of tests, run against two implementations. Your starter must
/// pass exactly what the model answer passes.
/// </summary>
public abstract class PageAccessCheckerTests
{
    protected abstract IPageAccessChecker CreateChecker();

    private static readonly KataPage[] Site =
    [
        new("/", null, []),
        new("/reports", "/", ["analyst", "manager"]),
        new("/reports/finance", "/reports", ["manager"]),
        new("/admin", "/", ["it"]),
        new("/help", null, null),
    ];

    private IReadOnlyList<string> PagesFor(KataUser user, IEnumerable<KataPage>? pages = null) =>
        CreateChecker().AccessiblePagesByUser([user], pages ?? Site)[user.Name];

    [Fact]
    public void Pages_without_required_roles_are_open_to_everyone()
    {
        Assert.Equal(["/", "/help"], PagesFor(new KataUser("guest", [])));
    }

    [Fact]
    public void Any_one_required_role_is_enough()
    {
        Assert.Equal(["/", "/help", "/reports"], PagesFor(new KataUser("ana", ["analyst"])));
    }

    [Fact]
    public void A_child_needs_its_parent()
    {
        var pages = new KataPage[] { new("/", null, []), new("/reports", "/", ["analyst"]), new("/reports/finance", "/reports", ["manager"]) };

        // manager without analyst: finance is allowed by role but sits behind /reports.
        Assert.Equal(["/"], PagesFor(new KataUser("mo", ["manager"]), pages));
    }

    [Fact]
    public void Admin_sees_every_page()
    {
        Assert.Equal(["/", "/admin", "/help", "/reports", "/reports/finance"], PagesFor(new KataUser("root", ["admin"])));
    }

    [Fact]
    public void Role_names_and_paths_ignore_case_and_trailing_slashes()
    {
        var pages = new KataPage[] { new("/", null, []), new("/Reports/", "/", ["Analyst"]), new("/reports/finance", "/REPORTS", [" analyst "]) };

        Assert.Equal(["/", "/Reports/", "/reports/finance"], PagesFor(new KataUser("ana", ["ANALYST"]), pages));
    }

    [Fact]
    public void Null_role_list_is_treated_as_no_roles()
    {
        Assert.Equal(["/", "/help"], PagesFor(new KataUser("nobody", null)));
    }

    [Fact]
    public void Missing_parent_denies_the_page_even_to_admin()
    {
        var pages = new KataPage[] { new("/orphan", "/gone", []) };

        Assert.Empty(PagesFor(new KataUser("root", ["admin"]), pages));
    }

    [Fact]
    public void A_parent_cycle_denies_every_page_on_it()
    {
        var pages = new KataPage[] { new("/a", "/b", []), new("/b", "/a", []), new("/c", "/a", []) };

        Assert.Empty(PagesFor(new KataUser("root", ["admin"]), pages));
    }

    [Fact]
    public void The_first_definition_of_a_duplicated_path_wins()
    {
        var pages = new KataPage[] { new("/x", null, []), new("/X", null, ["it"]) };

        Assert.Equal(["/x"], PagesFor(new KataUser("guest", []), pages));
    }

    [Fact]
    public void Every_user_gets_an_entry_even_if_it_is_empty()
    {
        var result = CreateChecker().AccessiblePagesByUser([new KataUser("a", []), new KataUser("b", ["it"])], []);

        Assert.Equal(2, result.Count);
        Assert.All(result.Values, Assert.Empty);
    }

    [Fact]
    public void No_users_means_an_empty_result()
    {
        Assert.Empty(CreateChecker().AccessiblePagesByUser([], Site));
    }
}

public sealed class SolutionTests : PageAccessCheckerTests
{
    protected override IPageAccessChecker CreateChecker() => new SolutionPageAccessChecker();
}

[Trait("Category", "Starter")]
public sealed class StarterTests : PageAccessCheckerTests
{
    protected override IPageAccessChecker CreateChecker() => new StarterPageAccessChecker();
}
