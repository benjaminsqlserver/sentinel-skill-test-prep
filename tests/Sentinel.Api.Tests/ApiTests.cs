using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Sentinel.Contracts;

namespace Sentinel.Api.Tests;

public partial class ApiTests(SentinelApiFactory factory) : IClassFixture<SentinelApiFactory>
{
    private const string Ada = "ada.okafor@sentinel.dev";      // Administrator
    private const string Femi = "femi.bello@sentinel.dev";     // Auditor: admin.users, not .manage
    private const string Chen = "chen.wei@sentinel.dev";       // Operations
    private const string Dana = "dana.smith@sentinel.dev";

    // ---- Authentication ----

    [Fact]
    public async Task Me_requires_authentication()
    {
        var response = await factory.ClientFor(null).GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task An_unknown_demo_user_is_not_authenticated()
    {
        var response = await factory.ClientFor("nobody@example.com").GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_returns_the_signed_in_user()
    {
        var me = await factory.ClientFor(Ada).GetFromJsonAsync<CurrentUserDto>("/api/me");

        Assert.Equal("Ada Okafor", me!.DisplayName);
        Assert.Equal("Africa/Lagos", me.TimeZoneId);
        Assert.Equal(["*"], me.Permissions);
    }

    // ---- Authorisation: the endpoint, not just the menu ----

    [Theory]
    [InlineData(Ada, "/api/users", HttpStatusCode.OK)]
    [InlineData(Femi, "/api/users", HttpStatusCode.OK)]
    [InlineData(Chen, "/api/users", HttpStatusCode.Forbidden)]
    [InlineData(Femi, "/api/roles", HttpStatusCode.Forbidden)] // auditors see users, not roles
    public async Task Admin_reads_are_guarded_by_permission(string user, string url, HttpStatusCode expected)
    {
        var response = await factory.ClientFor(user).GetAsync(url);

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task Reading_a_resource_does_not_imply_changing_it()
    {
        var response = await factory.ClientFor(Femi).PostAsJsonAsync("/api/users",
            new CreateUserRequest("new@sentinel.dev", "New", "Africa/Lagos"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---- Results to HTTP ----

    [Fact]
    public async Task Validation_errors_are_a_400_problem_with_one_entry_per_field()
    {
        var response = await factory.ClientFor(Ada).PostAsJsonAsync("/api/users", new CreateUserRequest("", "", "Africa/Lagos"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var errors = problem.RootElement.GetProperty("errors");
        Assert.True(errors.TryGetProperty("Email", out _));
        Assert.True(errors.TryGetProperty("DisplayName", out _));
    }

    [Fact]
    public async Task Business_rule_failures_carry_their_error_code()
    {
        var response = await factory.ClientFor(Ada).PostAsJsonAsync("/api/users",
            new CreateUserRequest("someone@sentinel.dev", "Someone", "Mars/Base"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("User.UnknownTimeZone", problem.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Creating_a_user_returns_201_with_a_location()
    {
        var response = await factory.ClientFor(Ada).PostAsJsonAsync("/api/users",
            new CreateUserRequest("kemi.ade@sentinel.dev", "Kemi Ade", "Africa/Lagos"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CreatedResponse>();
        Assert.Equal($"/api/users/{created!.Id}", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Unknown_ids_are_404()
    {
        var response = await factory.ClientFor(Ada).GetAsync($"/api/users/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- Page access over HTTP ----

    [Fact]
    public async Task Anonymous_navigation_is_the_public_pages()
    {
        var menu = await factory.ClientFor(null).GetFromJsonAsync<List<NavigationNodeDto>>("/api/me/navigation");

        Assert.Equal(["/help", "/status"], menu!.Select(n => n.Path));
    }

    [Fact]
    public async Task The_guard_endpoint_explains_a_denial()
    {
        var verdict = await factory.ClientFor(Chen).GetFromJsonAsync<PageAccessDto>("/api/me/page-access?path=/admin/users");

        Assert.False(verdict!.IsAllowed);
        Assert.Equal("ParentDenied", verdict.Reason);
    }

    // ---- The wire format for dates (SUP-4172, Chapter 22) ----

    [Fact]
    public async Task Every_timestamp_on_the_wire_is_utc_with_a_z()
    {
        var json = await factory.ClientFor(Dana).GetStringAsync("/api/me/security");

        var timestamps = TimestampProperty().Matches(json).Select(m => m.Groups["value"].Value).ToList();

        Assert.NotEmpty(timestamps);
        Assert.All(timestamps, t => Assert.EndsWith("Z", t));
    }

    [Fact]
    public async Task Acknowledging_a_finding_over_http_returns_204_and_lowers_the_risk()
    {
        var client = factory.ClientFor(Dana);
        var before = await client.GetFromJsonAsync<AccountSecurityStatusDto>("/api/me/security");
        var finding = before!.Findings.First(f => f.Kind == "NewDevice");

        var response = await client.PostAsync($"/api/me/security/findings/{Uri.EscapeDataString(finding.Key)}/acknowledge", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var after = await client.GetFromJsonAsync<AccountSecurityStatusDto>("/api/me/security");
        Assert.DoesNotContain(after!.Findings, f => f.Key == finding.Key);
    }

    [GeneratedRegex("\"(occurredAtUtc|assessedAtUtc)\":\"(?<value>[^\"]+)\"")]
    private static partial Regex TimestampProperty();
}
