namespace AccessLogKata;

public sealed record UserActivity(int Allowed, int Denied, double AverageResponseMs);

public sealed record LogSummary(
    IReadOnlyDictionary<string, UserActivity> ByUser,
    string? MostDeniedPath,
    IReadOnlyList<int> MalformedLines);

public interface IAccessLogSummariser
{
    LogSummary Summarise(IEnumerable<string> lines);
}
