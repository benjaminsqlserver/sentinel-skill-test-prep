using AccessLogKata.Solution;
using AccessLogKata.Starter;

namespace AccessLogKata.Tests;

public abstract class AccessLogSummariserTests
{
    protected abstract IAccessLogSummariser Create();

    [Fact]
    public void Counts_and_averages_per_user()
    {
        var summary = Create().Summarise(
        [
            "2026-10-24T08:00:00Z,ada,/reports,ALLOW,100",
            "2026-10-24T08:01:00Z,Ada,/admin,DENY,50",
            "2026-10-24T08:02:00+01:00,bola,/reports,allow,30",
        ]);

        Assert.Equal(new UserActivity(1, 1, 75), summary.ByUser["ada"]);
        Assert.Equal(new UserActivity(1, 0, 30), summary.ByUser["bola"]);
    }

    [Fact]
    public void Most_denied_path_breaks_ties_alphabetically()
    {
        var summary = Create().Summarise(
        [
            "2026-10-24T08:00:00Z,a,/z,DENY,1",
            "2026-10-24T08:00:00Z,a,/b,DENY,1",
            "2026-10-24T08:00:00Z,a,/z,ALLOW,1",
        ]);

        Assert.Equal("/b", summary.MostDeniedPath);
    }

    [Fact]
    public void No_denials_means_no_most_denied_path()
    {
        Assert.Null(Create().Summarise(["2026-10-24T08:00:00Z,a,/z,ALLOW,1"]).MostDeniedPath);
    }

    [Fact]
    public void Malformed_lines_are_reported_by_number_and_otherwise_ignored()
    {
        var summary = Create().Summarise(
        [
            "# header comment",
            "2026-10-24T08:00:00Z,ada,/reports,ALLOW,100",
            "2026-10-24T08:00:00,ada,/reports,ALLOW,100",   // 3: no zone
            "not,a,log,line",                                // 4: four fields
            "2026-10-24T08:00:00Z,ada,/reports,MAYBE,100",  // 5: bad decision
            "2026-10-24T08:00:00Z,ada,/reports,ALLOW,-5",   // 6: negative
            "2026-10-24T08:00:00Z,ada,/reports,ALLOW,1,000",// 7: six fields
            "",
            "2026-10-24T08:00:00Z,,/reports,ALLOW,1",       // 9: no user
            "2026-10-24T08:00:00Z,ada,reports,ALLOW,1",     // 10: not a path
        ]);

        Assert.Equal([3, 4, 5, 6, 7, 9, 10], summary.MalformedLines);
        Assert.Equal(new UserActivity(1, 0, 100), Assert.Single(summary.ByUser).Value);
    }

    [Fact]
    public void An_empty_log_is_an_empty_summary()
    {
        var summary = Create().Summarise([]);

        Assert.Empty(summary.ByUser);
        Assert.Empty(summary.MalformedLines);
        Assert.Null(summary.MostDeniedPath);
    }
}

public sealed class SolutionTests : AccessLogSummariserTests
{
    protected override IAccessLogSummariser Create() => new SolutionAccessLogSummariser();
}

[Trait("Category", "Starter")]
public sealed class StarterTests : AccessLogSummariserTests
{
    protected override IAccessLogSummariser Create() => new StarterAccessLogSummariser();
}
