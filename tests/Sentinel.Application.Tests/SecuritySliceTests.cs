using Microsoft.EntityFrameworkCore;
using Sentinel.Application.Security;
using Sentinel.Application.Security.AcknowledgeFinding;
using Sentinel.Application.Security.GetAccountSecurityStatus;
using Sentinel.Application.Security.GetSignInHistory;
using Sentinel.Application.Security.RecordSignIn;
using Sentinel.Application.Users.CreateUser;
using Sentinel.Domain.Common;

namespace Sentinel.Application.Tests;

public class SecuritySliceTests
{
    [Theory]
    [InlineData("ada.okafor@sentinel.dev", "Low")]
    [InlineData("bola.adeyemi@sentinel.dev", "High")]
    [InlineData("chen.wei@sentinel.dev", "Medium")]
    [InlineData("dana.smith@sentinel.dev", "Medium")]
    [InlineData("emeka.nwosu@sentinel.dev", "None")]
    public async Task Each_seeded_story_produces_its_risk_level(string email, string expected)
    {
        await using var host = await TestHost.CreateAsync(seed: true);

        var status = (await host.Send(new GetAccountSecurityStatusQuery(await host.UserIdAsync(email)))).Value;

        Assert.Equal(expected, status.RiskLevel);
    }


    /// <summary>Regression test for SUP-4172 (Chapter 22): what comes back from the database is UTC.</summary>
    [Fact]
    public async Task Sign_in_times_come_back_from_the_database_as_utc()
    {
        await using var host = await TestHost.CreateAsync(seed: true);

        var status = (await host.Send(new GetAccountSecurityStatusQuery(await host.UserIdAsync("ada.okafor@sentinel.dev")))).Value;

        Assert.All(status.RecentSignIns, s => Assert.Equal(DateTimeKind.Utc, s.OccurredAtUtc.Kind));
        Assert.All(status.Findings, f => Assert.Equal(DateTimeKind.Utc, f.OccurredAtUtc.Kind));
    }

    [Fact]
    public async Task Flagged_rows_are_exactly_the_evidence_for_the_findings()
    {
        await using var host = await TestHost.CreateAsync(seed: true);

        var status = (await host.Send(new GetAccountSecurityStatusQuery(await host.UserIdAsync("bola.adeyemi@sentinel.dev")))).Value;

        var evidence = status.Findings.SelectMany(f => f.AttemptIds).ToHashSet();
        Assert.All(status.RecentSignIns, s => Assert.Equal(evidence.Contains(s.Id), s.IsFlagged));
        Assert.Contains(status.RecentSignIns, s => s.IsFlagged);
    }

    [Fact]
    public async Task Acknowledging_a_finding_lowers_the_risk_and_is_idempotent()
    {
        await using var host = await TestHost.CreateAsync(seed: true);
        var dana = await host.UserIdAsync("dana.smith@sentinel.dev");
        var before = (await host.Send(new GetAccountSecurityStatusQuery(dana))).Value;
        var newCountry = before.Findings.Single(f => f.Kind == "NewCountry");

        Assert.True((await host.Send(new AcknowledgeFindingCommand(dana, newCountry.Key))).IsSuccess);
        Assert.True((await host.Send(new AcknowledgeFindingCommand(dana, newCountry.Key))).IsSuccess);

        var after = (await host.Send(new GetAccountSecurityStatusQuery(dana))).Value;
        Assert.Equal("Medium", before.RiskLevel);
        Assert.Equal("Low", after.RiskLevel);
        Assert.False(after.IsCompromiseSuspected);
    }

    [Fact]
    public async Task A_finding_the_user_cannot_see_cannot_be_acknowledged()
    {
        await using var host = await TestHost.CreateAsync(seed: true);
        var dana = await host.UserIdAsync("dana.smith@sentinel.dev");

        var result = await host.Send(new AcknowledgeFindingCommand(dana, "brute-force:made-up"));

        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task Recording_a_sign_in_resolves_the_location_and_stores_utc()
    {
        await using var host = await TestHost.CreateAsync();
        await host.Send(new CreateUserCommand("ada@example.com", "Ada", "Africa/Lagos"));

        // 13:00 in Lagos is 12:00 UTC.
        var lagosTime = new DateTimeOffset(2026, 10, 25, 13, 0, 0, TimeSpan.FromHours(1));
        var id = (await host.Send(new RecordSignInCommand("ada@example.com", lagosTime, "102.89.34.7", "curl/8", true, null))).Value;

        var saved = await host.Query(db => db.SignInAttempts.SingleAsync(a => a.Id == id));
        Assert.Equal(new DateTime(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc), saved.OccurredAtUtc);
        Assert.Equal(DateTimeKind.Utc, saved.OccurredAtUtc.Kind);
        Assert.Equal("Lagos, NG", saved.Location.ToString());
    }

    [Fact]
    public async Task Sign_ins_from_the_future_and_for_unknown_users_are_rejected()
    {
        await using var host = await TestHost.CreateAsync();
        await host.Send(new CreateUserCommand("ada@example.com", "Ada", "Africa/Lagos"));
        var tomorrow = new DateTimeOffset(TestHost.Now.AddDays(1));

        Assert.Equal("SignIn.InFuture", (await host.Send(new RecordSignInCommand("ada@example.com", tomorrow, "1.1.1.1", null, true, null))).Error!.Code);
        Assert.Equal(ErrorType.NotFound, (await host.Send(new RecordSignInCommand("who@example.com", TestHost.Now, "1.1.1.1", null, true, null))).Error!.Type);
        Assert.Equal(ErrorType.Validation, (await host.Send(new RecordSignInCommand("ada@example.com", TestHost.Now, "1.1.1.1", null, false, "Bananas"))).Error!.Type);
    }

    [Fact]
    public async Task History_is_paged_newest_first()
    {
        await using var host = await TestHost.CreateAsync(seed: true);
        var tunde = await host.UserIdAsync("tunde.bakare@sentinel.dev");

        var first = (await host.Send(new GetSignInHistoryQuery(tunde, 1, 10))).Value;
        var second = (await host.Send(new GetSignInHistoryQuery(tunde, 2, 10))).Value;

        Assert.Equal(10, first.Items.Count);
        Assert.True(first.Items[^1].OccurredAtUtc >= second.Items[0].OccurredAtUtc);
        Assert.Equal(first.Items.OrderByDescending(i => i.OccurredAtUtc).Select(i => i.Id), first.Items.Select(i => i.Id));
        Assert.True(first.TotalCount > 500);
    }
}
