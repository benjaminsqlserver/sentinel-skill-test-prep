using Sentinel.Domain.Access;
using Sentinel.Domain.Common;
using Sentinel.Domain.Users;

namespace Sentinel.Domain.Tests.Users;

public class UserTests
{
    private static readonly DateTime Now = History.Now;

    private static User NewUser() =>
        User.Create(EmailAddress.Create("ada@example.com").Value, "Ada", "Africa/Lagos", Now).Value;

    [Theory]
    [InlineData(" Ada@Example.COM ", "ada@example.com")]
    [InlineData("first.last+tag@sub.example.org", "first.last+tag@sub.example.org")]
    public void Email_is_normalised(string input, string expected)
    {
        Assert.Equal(expected, EmailAddress.Create(input).Value.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ada")]
    [InlineData("ada@localhost")]
    [InlineData("Ada <ada@example.com>")]
    public void Invalid_emails_are_rejected(string input)
    {
        Assert.True(EmailAddress.Create(input).IsFailure);
    }

    [Fact]
    public void Unknown_time_zones_are_rejected()
    {
        var result = User.Create(EmailAddress.Create("a@b.com").Value, "A", "Mars/Olympus_Mons", Now);

        Assert.Equal("User.UnknownTimeZone", result.Error!.Code);
    }

    [Fact]
    public void A_new_user_is_active_and_trimmed()
    {
        var user = User.Create(EmailAddress.Create("a@b.com").Value, "  Ada  ", "Europe/London", Now).Value;

        Assert.Equal(UserStatus.Active, user.Status);
        Assert.Equal("Ada", user.DisplayName);
    }

    [Fact]
    public void A_role_cannot_be_assigned_twice()
    {
        var user = NewUser();
        var roleId = Guid.NewGuid();

        Assert.True(user.AssignRole(roleId, Now).IsSuccess);
        Assert.Equal(ErrorType.Conflict, user.AssignRole(roleId, Now).Error!.Type);
        Assert.Single(user.Roles);
    }

    [Fact]
    public void Revoking_a_role_the_user_does_not_hold_is_not_found()
    {
        Assert.Equal(ErrorType.NotFound, NewUser().RevokeRole(Guid.NewGuid()).Error!.Type);
    }

    [Fact]
    public void Setting_an_override_replaces_any_existing_one_for_that_permission()
    {
        var user = NewUser();
        var export = Permission.From("reports.finance.export");

        user.SetOverride(export, PermissionEffect.Allow);
        user.SetOverride(export, PermissionEffect.Deny);

        var only = Assert.Single(user.Overrides);
        Assert.Equal(PermissionEffect.Deny, only.Effect);
    }

    [Fact]
    public void Suspension_needs_a_reason_and_cannot_be_repeated()
    {
        var user = NewUser();

        Assert.True(user.Suspend(" ").IsFailure);
        Assert.True(user.Suspend("Left the company").IsSuccess);
        Assert.Equal(UserErrors.AlreadySuspended, user.Suspend("Again").Error);
        Assert.Equal("Left the company", user.SuspensionReason);
    }

    [Fact]
    public void Reactivation_clears_the_reason()
    {
        var user = NewUser();
        user.Suspend("Left");

        Assert.True(user.Reactivate().IsSuccess);
        Assert.Null(user.SuspensionReason);
        Assert.Equal(UserErrors.NotSuspended, user.Reactivate().Error);
    }
}

public class ResultTests
{
    [Fact]
    public void Reading_the_value_of_a_failure_throws()
    {
        Result<int> failure = Error.NotFound("X", "missing");

        Assert.Throws<InvalidOperationException>(() => failure.Value);
    }

    [Fact]
    public void Values_and_errors_convert_implicitly()
    {
        Result<int> success = 42;
        Result<int> failure = Error.Conflict("X", "taken");

        Assert.Equal(42, success.Value);
        Assert.Equal(ErrorType.Conflict, failure.Error!.Type);
    }
}
