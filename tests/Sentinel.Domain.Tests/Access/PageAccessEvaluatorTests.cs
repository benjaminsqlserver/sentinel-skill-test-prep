using Sentinel.Domain.Access;
using Sentinel.Domain.Pages;

namespace Sentinel.Domain.Tests.Access;

public class PageAccessEvaluatorTests
{
    private static PageAccessDecision Decide(AccessSubject subject, PageTree tree, string path) =>
        PageAccessEvaluator.EvaluatePath(subject, tree.Pages, path)
        ?? throw new InvalidOperationException($"No page at {path}");

    // ---- The happy paths ----

    [Fact]
    public void Page_without_a_permission_is_allowed_for_any_active_user()
    {
        var decision = Decide(Subjects.Active(), PageTree.Standard(), "/account");

        Assert.True(decision.IsAllowed);
        Assert.Equal(AccessReason.AuthenticatedUser, decision.Reason);
    }

    [Fact]
    public void Page_is_allowed_when_a_grant_covers_its_permission()
    {
        var decision = Decide(Subjects.Active("reports.view", "reports.finance"), PageTree.Standard(), "/reports/finance");

        Assert.True(decision.IsAllowed);
        Assert.Equal(AccessReason.PermissionGranted, decision.Reason);
    }

    [Fact]
    public void Wildcard_grant_covers_every_page_beneath_it()
    {
        var subject = Subjects.Active("reports.view", "reports.*");

        Assert.True(Decide(subject, PageTree.Standard(), "/reports/finance").IsAllowed);
        Assert.True(Decide(subject, PageTree.Standard(), "/reports/finance/export").IsAllowed);
        Assert.True(Decide(subject, PageTree.Standard(), "/reports/operations").IsAllowed);
    }

    [Fact]
    public void Wildcard_grant_does_not_cover_the_parent_permission_itself()
    {
        // A landing page that requires "reports" itself. The grant covers
        // everything beneath reports, but not reports, and the children sit
        // behind it. (In the standard tree the landing page requires
        // "reports.view", which reports.* does cover. Writing this test the
        // first time against that tree is how we learnt the difference.)
        var tree = new PageTree()
            .Add("/reports", permission: "reports")
            .Add("/reports/finance", "/reports", "reports.finance");
        var subject = Subjects.Active("reports.*");

        Assert.Equal(AccessReason.MissingPermission, Decide(subject, tree, "/reports").Reason);
        Assert.Equal(AccessReason.ParentDenied, Decide(subject, tree, "/reports/finance").Reason);
    }

    [Fact]
    public void Superuser_wildcard_allows_everything()
    {
        var decisions = PageAccessEvaluator.EvaluateAll(Subjects.Active("*"), PageTree.Standard().Pages);

        Assert.All(decisions, d => Assert.True(d.IsAllowed));
    }

    // ---- Denials, each with the right reason ----

    [Fact]
    public void Page_is_denied_when_no_grant_covers_its_permission()
    {
        var decision = Decide(Subjects.Active("reports.view"), PageTree.Standard(), "/reports/finance");

        Assert.False(decision.IsAllowed);
        Assert.Equal(AccessReason.MissingPermission, decision.Reason);
    }

    [Fact]
    public void Explicit_deny_beats_a_role_grant()
    {
        var subject = Subjects.ActiveWithDenies(
            granted: ["reports.view", "reports.finance", "reports.finance.*"],
            denied: ["reports.finance.export"]);

        Assert.True(Decide(subject, PageTree.Standard(), "/reports/finance").IsAllowed);
        Assert.Equal(AccessReason.ExplicitlyDenied, Decide(subject, PageTree.Standard(), "/reports/finance/export").Reason);
    }

    [Fact]
    public void Wildcard_deny_beats_a_concrete_grant()
    {
        var subject = Subjects.ActiveWithDenies(granted: ["reports.view", "reports.finance"], denied: ["reports.*"]);

        // reports.* covers reports.view too, so the deny bites at the top of
        // the branch and everything beneath it follows.
        Assert.Equal(AccessReason.ExplicitlyDenied, Decide(subject, PageTree.Standard(), "/reports").Reason);
        Assert.Equal(AccessReason.ParentDenied, Decide(subject, PageTree.Standard(), "/reports/finance").Reason);
    }

    [Fact]
    public void Child_is_denied_when_its_parent_is_denied_even_if_the_child_permission_is_held()
    {
        // Holds the finance permission but not reports.view.
        var decision = Decide(Subjects.Active("reports.finance"), PageTree.Standard(), "/reports/finance");

        Assert.False(decision.IsAllowed);
        Assert.Equal(AccessReason.ParentDenied, decision.Reason);
    }

    [Fact]
    public void Denial_propagates_down_every_level()
    {
        var decision = Decide(Subjects.Active("reports.finance", "reports.finance.export"), PageTree.Standard(), "/reports/finance/export");

        Assert.Equal(AccessReason.ParentDenied, decision.Reason);
    }

    // ---- Who is asking ----

    [Fact]
    public void Anonymous_subject_sees_public_pages_only()
    {
        var decisions = PageAccessEvaluator.EvaluateAll(AccessSubject.Anonymous, PageTree.Standard().Pages);

        var allowed = decisions.Where(d => d.IsAllowed).Select(d => d.Page.Path);
        Assert.Equal(["/help"], allowed);
        Assert.All(decisions.Where(d => !d.IsAllowed), d => Assert.Equal(AccessReason.NotAuthenticated, d.Reason));
    }

    [Fact]
    public void Suspended_subject_loses_everything_but_public_pages_whatever_they_were_granted()
    {
        var decisions = PageAccessEvaluator.EvaluateAll(Subjects.Suspended("*"), PageTree.Standard().Pages);

        Assert.Equal(["/help"], decisions.Where(d => d.IsAllowed).Select(d => d.Page.Path));
        Assert.All(decisions.Where(d => !d.IsAllowed), d => Assert.Equal(AccessReason.UserSuspended, d.Reason));
    }

    [Fact]
    public void Public_page_under_a_denied_parent_is_still_allowed()
    {
        var tree = PageTree.Standard().Add("/reports/glossary", "/reports", isPublic: true);

        var decision = Decide(AccessSubject.Anonymous, tree, "/reports/glossary");

        Assert.True(decision.IsAllowed);
        Assert.Equal(AccessReason.PublicPage, decision.Reason);
    }

    // ---- Bad data: fail closed, never throw ----

    [Fact]
    public void Orphaned_page_is_denied()
    {
        var orphan = Page.Create("/lost", "Lost", parentId: Guid.NewGuid()).Value;

        var decision = PageAccessEvaluator.EvaluateAll(Subjects.Active("*"), [orphan]).Single();

        Assert.False(decision.IsAllowed);
        Assert.Equal(AccessReason.OrphanedPage, decision.Reason);
    }

    [Fact]
    public void Pages_on_a_parent_cycle_are_denied_and_pages_below_it_are_denied_as_children()
    {
        var a = Page.Create("/a", "A").Value;
        var b = Page.Create("/b", "B", parentId: a.Id).Value;
        var c = Page.Create("/c", "C", parentId: b.Id).Value;
        a.MoveUnder(b.Id); // a -> b -> a

        var decisions = PageAccessEvaluator.EvaluateAll(Subjects.Active("*"), [a, b, c])
            .ToDictionary(d => d.Page.Path, d => d.Reason);

        Assert.Equal(AccessReason.CyclicHierarchy, decisions["/a"]);
        Assert.Equal(AccessReason.CyclicHierarchy, decisions["/b"]);
        Assert.Equal(AccessReason.ParentDenied, decisions["/c"]);
    }

    [Fact]
    public void A_page_cannot_be_made_its_own_parent()
    {
        var page = Page.Create("/a", "A").Value;

        Assert.True(page.MoveUnder(page.Id).IsFailure);
        Assert.Null(page.ParentId);
    }

    [Fact]
    public void Duplicate_page_ids_are_a_programming_error()
    {
        var page = Page.Create("/a", "A").Value;

        Assert.Throws<ArgumentException>(() => PageAccessEvaluator.EvaluateAll(Subjects.Active(), [page, page]));
    }

    [Fact]
    public void A_very_deep_tree_does_not_overflow_the_stack()
    {
        var tree = new PageTree().Add("/l0");
        for (var i = 1; i < 2_000; i++)
            tree.Add($"/l{i}", $"/l{i - 1}");

        var decisions = PageAccessEvaluator.EvaluateAll(Subjects.Active(), tree.Pages);

        Assert.Equal(2_000, decisions.Count);
        Assert.All(decisions, d => Assert.True(d.IsAllowed));
    }

    // ---- The API's contract ----

    [Fact]
    public void EvaluateAll_returns_one_decision_per_page_in_input_order()
    {
        var pages = PageTree.Standard().Pages.Reverse().ToList();

        var decisions = PageAccessEvaluator.EvaluateAll(Subjects.Active(), pages);

        Assert.Equal(pages.Select(p => p.Id), decisions.Select(d => d.Page.Id));
    }

    [Fact]
    public void EvaluateAll_with_no_pages_returns_nothing()
    {
        Assert.Empty(PageAccessEvaluator.EvaluateAll(Subjects.Active("*"), []));
    }

    [Theory]
    [InlineData("/Reports/Finance")]
    [InlineData("/reports/finance/")]
    [InlineData("  /reports/finance  ")]
    public void EvaluatePath_normalises_the_path(string path)
    {
        var decision = PageAccessEvaluator.EvaluatePath(Subjects.Active("reports.view", "reports.finance"), PageTree.Standard().Pages, path);

        Assert.NotNull(decision);
        Assert.Equal("/reports/finance", decision.Page.Path);
    }

    [Theory]
    [InlineData("/does-not-exist")]
    [InlineData("reports")]    // not a path
    [InlineData("")]
    [InlineData("/a//b")]
    public void EvaluatePath_returns_null_for_unknown_or_invalid_paths(string path)
    {
        Assert.Null(PageAccessEvaluator.EvaluatePath(Subjects.Active("*"), PageTree.Standard().Pages, path));
    }
}
