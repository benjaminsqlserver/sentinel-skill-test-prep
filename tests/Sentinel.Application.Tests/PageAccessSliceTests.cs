using Sentinel.Application.Pages.CheckPageAccess;
using Sentinel.Application.Pages.CreatePage;
using Sentinel.Application.Pages.GetNavigation;
using Sentinel.Application.Pages.GetPageAccess;
using Sentinel.Application.Roles.GetRoles;
using Sentinel.Application.Roles.RolePermissions;
using Sentinel.Application.Users.RoleAssignment;
using Sentinel.Contracts;
using Sentinel.Domain.Common;

namespace Sentinel.Application.Tests;

public class PageAccessSliceTests
{
    private static IEnumerable<string> Flatten(IEnumerable<NavigationNodeDto> nodes) =>
        nodes.SelectMany(n => Flatten(n.Children).Prepend(n.Path));

    [Fact]
    public async Task The_menu_contains_exactly_the_allowed_pages()
    {
        await using var host = await TestHost.CreateAsync(seed: true);
        var emeka = await host.UserIdAsync("emeka.nwosu@sentinel.dev");

        var menu = (await host.Send(new GetNavigationQuery(emeka))).Value;

        Assert.Equal(
            ["/", "/account", "/account/security", "/reports", "/reports/finance", "/billing", "/help", "/status"],
            Flatten(menu));
    }

    [Fact]
    public async Task Anonymous_visitors_get_the_public_pages()
    {
        await using var host = await TestHost.CreateAsync(seed: true);

        var menu = (await host.Send(new GetNavigationQuery(null))).Value;

        Assert.Equal(["/help", "/status"], Flatten(menu));
    }

    [Fact]
    public async Task An_explicit_deny_is_explained()
    {
        await using var host = await TestHost.CreateAsync(seed: true);
        var emeka = await host.UserIdAsync("emeka.nwosu@sentinel.dev");

        var verdict = (await host.Send(new CheckPageAccessQuery(emeka, "/reports/finance/export"))).Value;

        Assert.False(verdict.IsAllowed);
        Assert.Equal("ExplicitlyDenied", verdict.Reason);
        Assert.Equal("/reports/finance", verdict.ParentPath);
    }

    [Fact]
    public async Task Checking_an_unknown_page_is_not_found()
    {
        await using var host = await TestHost.CreateAsync(seed: true);

        var result = await host.Send(new CheckPageAccessQuery(null, "/nowhere"));

        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task The_access_explorer_lists_every_page_with_a_reason()
    {
        await using var host = await TestHost.CreateAsync(seed: true);
        var grace = await host.UserIdAsync("grace.eze@sentinel.dev"); // suspended

        var rows = (await host.Send(new GetPageAccessQuery(grace))).Value;

        Assert.Equal(15, rows.Count);
        Assert.All(rows.Where(r => !r.IsAllowed), r => Assert.Equal("UserSuspended", r.Reason));
        Assert.Equal(["/help", "/status"], rows.Where(r => r.IsAllowed).Select(r => r.Path));
    }

    [Fact]
    public async Task Granting_a_role_permission_is_saved_and_changes_access()
    {
        await using var host = await TestHost.CreateAsync(seed: true);
        var chen = await host.UserIdAsync("chen.wei@sentinel.dev");
        var operations = (await host.Send(new GetRolesQuery())).Value.Single(r => r.Name == "Operations");

        Assert.False((await host.Send(new CheckPageAccessQuery(chen, "/billing"))).Value.IsAllowed);

        Assert.True((await host.Send(new GrantPermissionCommand(operations.Id, "billing.view"))).IsSuccess);

        // A fresh scope reads from the database: proves the value comparer
        // noticed the list changed and EF Core actually wrote it.
        Assert.True((await host.Send(new CheckPageAccessQuery(chen, "/billing"))).Value.IsAllowed);
    }

    [Fact]
    public async Task Revoking_a_role_removes_its_pages()
    {
        await using var host = await TestHost.CreateAsync(seed: true);
        var femi = await host.UserIdAsync("femi.bello@sentinel.dev");
        var auditor = (await host.Send(new GetRolesQuery())).Value.Single(r => r.Name == "Auditor");

        await host.Send(new RevokeRoleCommand(femi, auditor.Id));

        var menu = Flatten((await host.Send(new GetNavigationQuery(femi))).Value).ToList();
        Assert.DoesNotContain("/admin", menu);
        Assert.DoesNotContain("/reports", menu);
    }

    [Fact]
    public async Task Pages_cannot_require_a_wildcard()
    {
        await using var host = await TestHost.CreateAsync(seed: true);

        var result = await host.Send(new CreatePageCommand("/reports/all", "All", "/reports", "reports.*", false, 0));

        Assert.Equal("Page.WildcardPermission", result.Error!.Code);
    }

    [Fact]
    public async Task A_new_page_must_have_an_existing_parent_and_a_unique_path()
    {
        await using var host = await TestHost.CreateAsync(seed: true);

        Assert.Equal(ErrorType.NotFound, (await host.Send(new CreatePageCommand("/x/y", "Y", "/x", null, false, 0))).Error!.Type);
        Assert.Equal(ErrorType.Conflict, (await host.Send(new CreatePageCommand("/Billing/", "Again", "/", null, false, 0))).Error!.Type);
        Assert.True((await host.Send(new CreatePageCommand("/billing/invoices", "Invoices", "/billing", "billing.view", false, 0))).IsSuccess);
    }
}
