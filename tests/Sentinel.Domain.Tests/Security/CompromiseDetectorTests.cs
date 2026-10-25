using Sentinel.Domain.Security;

namespace Sentinel.Domain.Tests.Security;

public class CompromiseDetectorTests
{
    private static readonly DateTime Now = History.Now;

    private static SecurityAssessment Assess(History history, IReadOnlySet<string>? acknowledged = null) =>
        CompromiseDetector.Assess(history.Attempts, Now, acknowledged);

    // ---- Nothing to see ----

    [Fact]
    public void No_history_is_clear()
    {
        var assessment = CompromiseDetector.Assess([], Now);

        Assert.Equal(Severity.None, assessment.RiskLevel);
        Assert.Empty(assessment.Findings);
        Assert.False(assessment.IsCompromiseSuspected);
    }

    [Fact]
    public void An_ordinary_history_is_clear()
    {
        var assessment = Assess(new History().WithOrdinaryBaseline().Success(Now.AddHours(-1)));

        Assert.Equal(Severity.None, assessment.RiskLevel);
    }

    [Fact]
    public void The_first_ever_sign_in_is_not_new_anything()
    {
        // No baseline yet: flagging it would flag every new account.
        var assessment = Assess(new History().Success(Now.AddHours(-1), Places.London, Agents.Phone));

        Assert.Empty(assessment.Findings);
    }

    // ---- Brute force ----

    [Fact]
    public void Failures_below_the_threshold_are_not_a_burst()
    {
        var history = new History().WithOrdinaryBaseline()
            .Failures(Now.AddHours(-2), count: 4, spacing: TimeSpan.FromMinutes(1));

        Assert.DoesNotContain(Assess(history).Findings, f => f.Kind == FindingKind.BruteForce);
    }

    [Fact]
    public void Threshold_failures_inside_the_window_are_a_medium_finding()
    {
        var history = new History().WithOrdinaryBaseline()
            .Failures(Now.AddHours(-2), count: 5, spacing: TimeSpan.FromMinutes(2), where: Places.Frankfurt);

        var finding = Assert.Single(Assess(history).Findings);

        Assert.Equal(FindingKind.BruteForce, finding.Kind);
        Assert.Equal(Severity.Medium, finding.Severity);
        Assert.Equal(5, finding.AttemptIds.Count);
        Assert.Contains("DE", finding.Summary);
    }

    [Fact]
    public void Failures_spread_more_thinly_than_the_window_allows_are_not_a_burst()
    {
        // Ten minutes apart: never more than two inside any fifteen minutes.
        var history = new History().WithOrdinaryBaseline()
            .Failures(Now.AddHours(-3), count: 8, spacing: TimeSpan.FromMinutes(10));

        Assert.DoesNotContain(Assess(history).Findings, f => f.Kind == FindingKind.BruteForce);
    }

    [Fact]
    public void A_success_straight_after_the_burst_makes_it_high_and_cites_the_success()
    {
        var history = new History().WithOrdinaryBaseline()
            .Failures(Now.AddHours(-2), count: 6, spacing: TimeSpan.FromSeconds(30))
            .Success(Now.AddHours(-2).AddMinutes(4));

        var finding = Assert.Single(Assess(history).Findings, f => f.Kind == FindingKind.BruteForce);

        Assert.Equal(Severity.High, finding.Severity);
        Assert.Contains(history.Last.Id, finding.AttemptIds);
        Assert.Equal(history.Last.OccurredAtUtc, finding.OccurredAtUtc);
    }

    [Fact]
    public void Two_separate_bursts_are_two_findings()
    {
        var history = new History().WithOrdinaryBaseline()
            .Failures(Now.AddDays(-3), count: 5, spacing: TimeSpan.FromMinutes(1))
            .Failures(Now.AddDays(-1), count: 5, spacing: TimeSpan.FromMinutes(1));

        Assert.Equal(2, Assess(history).Findings.Count(f => f.Kind == FindingKind.BruteForce));
    }

    // ---- Impossible travel ----

    [Fact]
    public void Lagos_then_London_forty_minutes_later_is_impossible_travel()
    {
        var history = new History().WithOrdinaryBaseline()
            .Success(Now.AddHours(-5), Places.Lagos)
            .Success(Now.AddHours(-5).AddMinutes(40), Places.London);

        var finding = Assert.Single(Assess(history).Findings, f => f.Kind == FindingKind.ImpossibleTravel);

        Assert.Equal(Severity.High, finding.Severity);
        Assert.Equal(2, finding.AttemptIds.Count);
        Assert.Contains("40 minutes", finding.Summary);
    }

    [Fact]
    public void New_York_then_London_a_day_later_is_plausible()
    {
        var history = new History().WithOrdinaryBaseline()
            .Success(Now.AddDays(-2), Places.NewYork)
            .Success(Now.AddDays(-1), Places.London);

        Assert.DoesNotContain(Assess(history).Findings, f => f.Kind == FindingKind.ImpossibleTravel);
    }

    [Fact]
    public void Short_hops_are_ignored_because_ip_geolocation_is_too_coarse()
    {
        var history = new History().WithOrdinaryBaseline()
            .Success(Now.AddHours(-1), Places.Lagos)
            .Success(Now.AddHours(-1).AddMinutes(1), Places.Ikeja);

        Assert.DoesNotContain(Assess(history).Findings, f => f.Kind == FindingKind.ImpossibleTravel);
    }

    [Fact]
    public void Simultaneous_sign_ins_far_apart_do_not_divide_by_zero()
    {
        var at = Now.AddHours(-1);
        var history = new History().WithOrdinaryBaseline()
            .Success(at, Places.Lagos)
            .Success(at, Places.London);

        Assert.Contains(Assess(history).Findings, f => f.Kind == FindingKind.ImpossibleTravel);
    }

    [Fact]
    public void Unknown_locations_are_skipped_rather_than_treated_as_zero_zero()
    {
        var history = new History().WithOrdinaryBaseline()
            .Success(Now.AddHours(-2), GeoLocation.Unknown)
            .Success(Now.AddHours(-1), Places.Lagos);

        Assert.DoesNotContain(Assess(history).Findings, f => f.Kind == FindingKind.ImpossibleTravel);
    }

    [Fact]
    public void Failed_attempts_do_not_count_as_travel()
    {
        var history = new History().WithOrdinaryBaseline()
            .Success(Now.AddHours(-2), Places.Lagos)
            .Failure(Now.AddHours(-2).AddMinutes(5), Places.London);

        Assert.DoesNotContain(Assess(history).Findings, f => f.Kind == FindingKind.ImpossibleTravel);
    }

    // ---- New country, new device ----

    [Fact]
    public void A_success_from_a_new_country_is_medium()
    {
        var history = new History().WithOrdinaryBaseline().Success(Now.AddDays(-1), Places.London);

        var finding = Assert.Single(Assess(history).Findings, f => f.Kind == FindingKind.NewCountry);

        Assert.Equal(Severity.Medium, finding.Severity);
        Assert.Contains("London, GB", finding.Summary);
    }

    [Fact]
    public void Only_the_first_sign_in_from_a_country_is_new()
    {
        var history = new History().WithOrdinaryBaseline()
            .Success(Now.AddDays(-3), Places.London)
            .Success(Now.AddDays(-2), Places.London);

        Assert.Single(Assess(history).Findings, f => f.Kind == FindingKind.NewCountry);
    }

    [Fact]
    public void A_failure_from_a_new_country_neither_raises_nor_establishes_it()
    {
        var history = new History().WithOrdinaryBaseline()
            .Failure(Now.AddDays(-2), Places.Frankfurt)
            .Success(Now.AddDays(-1), Places.Frankfurt);

        // The later success is still the first *successful* sign-in from DE.
        var finding = Assert.Single(Assess(history).Findings, f => f.Kind == FindingKind.NewCountry);
        Assert.Equal(history.Last.Id, Assert.Single(finding.AttemptIds));
    }

    [Fact]
    public void A_new_device_is_low()
    {
        var history = new History().WithOrdinaryBaseline().Success(Now.AddDays(-1), agent: Agents.Phone);

        var finding = Assert.Single(Assess(history).Findings);

        Assert.Equal(FindingKind.NewDevice, finding.Kind);
        Assert.Equal(Severity.Low, finding.Severity);
        Assert.Contains("Safari on iPhone", finding.Summary);
        Assert.False(Assess(history).IsCompromiseSuspected);
    }

    // ---- Windows, acknowledgements, robustness ----

    [Fact]
    public void Findings_older_than_the_reporting_window_are_not_reported_but_still_count_as_baseline()
    {
        var history = new History()
            .Success(Now.AddDays(-80), Places.Lagos)
            .Success(Now.AddDays(-60), Places.London)  // new country, but 60 days ago
            .Success(Now.AddDays(-1), Places.London);   // London is no longer new

        Assert.Empty(Assess(history).Findings);
    }

    [Fact]
    public void Acknowledged_findings_are_excluded_and_the_risk_level_drops()
    {
        var history = new History().WithOrdinaryBaseline()
            .Success(Now.AddDays(-1), Places.London, Agents.Phone);
        var before = Assess(history);
        var newCountry = before.Findings.Single(f => f.Kind == FindingKind.NewCountry);

        var after = Assess(history, new HashSet<string> { newCountry.Key });

        Assert.Equal(Severity.Medium, before.RiskLevel);
        Assert.Equal(Severity.Low, after.RiskLevel);
        Assert.DoesNotContain(after.Findings, f => f.Key == newCountry.Key);
    }

    [Fact]
    public void Finding_keys_are_stable_across_assessments()
    {
        var history = new History().WithOrdinaryBaseline().Success(Now.AddDays(-1), Places.London);

        Assert.Equal(
            Assess(history).Findings.Select(f => f.Key),
            Assess(history).Findings.Select(f => f.Key));
    }

    [Fact]
    public void Input_order_does_not_matter()
    {
        var history = new History().WithOrdinaryBaseline()
            .Success(Now.AddHours(-5), Places.Lagos)
            .Success(Now.AddHours(-5).AddMinutes(40), Places.London);

        var shuffled = history.Attempts.OrderBy(_ => Guid.NewGuid()).ToList();

        Assert.Equal(
            Assess(history).Findings.Select(f => f.Key),
            CompromiseDetector.Assess(shuffled, Now).Findings.Select(f => f.Key));
    }

    [Fact]
    public void Attempts_after_now_are_ignored()
    {
        var history = new History().WithOrdinaryBaseline().Success(Now.AddHours(1), Places.London);

        Assert.Empty(Assess(history).Findings);
    }

    [Fact]
    public void Findings_are_ordered_most_severe_first_then_newest_first()
    {
        var history = new History().WithOrdinaryBaseline()
            .Success(Now.AddDays(-3), agent: Agents.Phone)            // low
            .Success(Now.AddDays(-2), Places.Lagos)
            .Success(Now.AddDays(-2).AddMinutes(30), Places.London);  // high + medium

        var severities = Assess(history).Findings.Select(f => f.Severity).ToList();

        Assert.Equal(severities.OrderByDescending(s => s), severities);
        Assert.Equal(Severity.High, severities[0]);
    }

    [Fact]
    public void Now_must_be_utc()
    {
        Assert.Throws<ArgumentException>(() => CompromiseDetector.Assess([], DateTime.Now));
    }
}
