using Sentinel.Domain.Access;

namespace Sentinel.Domain.Tests.Access;

public class PermissionTests
{
    [Theory]
    [InlineData("reports.finance", "reports.finance")]
    [InlineData("  Reports.Finance ", "reports.finance")]
    [InlineData("reports.*", "reports.*")]
    [InlineData("*", "*")]
    [InlineData("a", "a")]
    [InlineData("billing.view-all", "billing.view-all")]
    public void Create_normalises_valid_names(string input, string expected)
    {
        var result = Permission.Create(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("reports.")]
    [InlineData(".reports")]
    [InlineData("reports..finance")]
    [InlineData("reports finance")]
    [InlineData("reports.*.finance")] // wildcard only at the end
    [InlineData("*.reports")]
    [InlineData("reports*")]
    public void Create_rejects_malformed_names(string? input)
    {
        var result = Permission.Create(input);

        Assert.True(result.IsFailure);
        Assert.Equal(Common.ErrorType.Validation, result.Error!.Type);
    }

    [Theory]
    [InlineData("reports.finance", "reports.finance", true)]
    [InlineData("reports.finance", "reports.operations", false)]
    [InlineData("reports.*", "reports.finance", true)]
    [InlineData("reports.*", "reports.finance.export", true)]  // any depth
    [InlineData("reports.*", "reports", false)]                // not the parent itself
    [InlineData("reports.*", "reportsx.finance", false)]       // prefix is segment-aware
    [InlineData("*", "anything.at.all", true)]
    [InlineData("reports.finance", "reports.finance.export", false)] // a concrete grant has no children
    public void Covers(string grant, string required, bool expected)
    {
        Assert.Equal(expected, Permission.From(grant).Covers(Permission.From(required)));
    }

    [Fact]
    public void Equality_is_by_value()
    {
        Assert.Equal(Permission.From("a.b"), Permission.From("A.B"));
        Assert.Equal(Permission.From("a.b").GetHashCode(), Permission.From("A.B").GetHashCode());
    }
}
