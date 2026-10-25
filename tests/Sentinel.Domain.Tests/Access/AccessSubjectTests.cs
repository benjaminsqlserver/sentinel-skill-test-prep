using Sentinel.Domain.Access;
using Sentinel.Domain.Roles;
using Sentinel.Domain.Users;

namespace Sentinel.Domain.Tests.Access;

public class AccessSubjectTests
{
    private static readonly DateTime Now = History.Now;

    private static User NewUser() =>
        User.Create(EmailAddress.Create("ada@example.com").Value, "Ada", "Africa/Lagos", Now).Value;

    private static Role NewRole(string name, params string[] permissions)
    {
        var role = Role.Create(name).Value;
        foreach (var p in permissions)
            role.Grant(Permission.From(p));
        return role;
    }

    [Fact]
    public void Only_roles_the_user_holds_contribute_grants()
    {
        var finance = NewRole("Finance", "reports.finance");
        var admin = NewRole("Admin", "*");
        var user = NewUser();
        user.AssignRole(finance.Id, Now);

        var subject = AccessSubject.For(user, [finance, admin]);

        Assert.True(subject.Has(Permission.From("reports.finance")));
        Assert.False(subject.Has(Permission.From("admin.users")));
    }

    [Fact]
    public void User_overrides_add_and_remove_permissions()
    {
        var finance = NewRole("Finance", "reports.finance", "reports.finance.*");
        var user = NewUser();
        user.AssignRole(finance.Id, Now);
        user.SetOverride(Permission.From("billing.view"), PermissionEffect.Allow);
        user.SetOverride(Permission.From("reports.finance.export"), PermissionEffect.Deny);

        var subject = AccessSubject.For(user, [finance]);

        Assert.True(subject.Has(Permission.From("billing.view")));
        Assert.False(subject.Has(Permission.From("reports.finance.export")));
        Assert.True(subject.Has(Permission.From("reports.finance")));
    }

    [Fact]
    public void Suspended_user_has_no_permissions()
    {
        var admin = NewRole("Admin", "*");
        var user = NewUser();
        user.AssignRole(admin.Id, Now);
        user.Suspend("Left");

        var subject = AccessSubject.For(user, [admin]);

        Assert.Equal(SubjectStatus.Suspended, subject.Status);
        Assert.False(subject.Has(Permission.From("anything")));
    }
}
