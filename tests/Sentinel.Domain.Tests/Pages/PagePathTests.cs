using Sentinel.Domain.Pages;

namespace Sentinel.Domain.Tests.Pages;

public class PagePathTests
{
    [Theory]
    [InlineData("/", "/")]
    [InlineData("///", "/")]
    [InlineData("/Admin/Users/", "/admin/users")]
    [InlineData("  /reports  ", "/reports")]
    [InlineData("/access-explorer", "/access-explorer")]
    public void Valid_paths_are_normalised(string input, string expected)
    {
        Assert.Equal(expected, PagePath.TryNormalise(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("admin")]
    [InlineData("/admin//users")]
    [InlineData("/admin users")]
    [InlineData("/admin?tab=1")]
    [InlineData("/-admin")]
    public void Invalid_paths_are_rejected(string? input)
    {
        Assert.Null(PagePath.TryNormalise(input));
    }

    [Theory]
    [InlineData("/admin/users", "/admin")]
    [InlineData("/admin", "/")]
    [InlineData("/", null)]
    public void ParentOf(string path, string? expected)
    {
        Assert.Equal(expected, PagePath.ParentOf(path));
    }

    [Fact]
    public void A_public_page_cannot_require_a_permission()
    {
        var result = Page.Create("/help", "Help", requiredPermission: Domain.Access.Permission.From("x"), isPublic: true);

        Assert.Equal(PageErrors.PublicPageWithPermission, result.Error);
    }
}
