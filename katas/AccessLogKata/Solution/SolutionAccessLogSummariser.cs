using System.Globalization;

namespace AccessLogKata.Solution;

/// <summary>
/// Model answer. Parsing and aggregating are separate steps: a line either
/// becomes a well-typed <see cref="Entry"/> or a line number in the error
/// list, and the aggregation never sees a string.
/// </summary>
public sealed class SolutionAccessLogSummariser : IAccessLogSummariser
{
    private sealed record Entry(DateTimeOffset At, string User, string Path, bool Allowed, int ResponseMs);

    private sealed class Totals
    {
        public int Allowed;
        public int Denied;
        public long ResponseMsSum;
    }

    public LogSummary Summarise(IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var byUser = new Dictionary<string, Totals>();
        var deniesByPath = new Dictionary<string, int>(StringComparer.Ordinal);
        var malformed = new List<int>();

        var lineNumber = 0;
        foreach (var line in lines)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
                continue;

            if (TryParse(line) is not { } entry)
            {
                malformed.Add(lineNumber);
                continue;
            }

            if (!byUser.TryGetValue(entry.User, out var totals))
                byUser[entry.User] = totals = new Totals();

            totals.ResponseMsSum += entry.ResponseMs; // long: a busy day of ints can overflow int
            if (entry.Allowed)
            {
                totals.Allowed++;
            }
            else
            {
                totals.Denied++;
                deniesByPath[entry.Path] = deniesByPath.GetValueOrDefault(entry.Path) + 1;
            }
        }

        var mostDenied = deniesByPath
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => kv.Key)
            .FirstOrDefault();

        var activity = byUser.ToDictionary(
            kv => kv.Key,
            kv => new UserActivity(kv.Value.Allowed, kv.Value.Denied,
                (double)kv.Value.ResponseMsSum / (kv.Value.Allowed + kv.Value.Denied)));

        return new LogSummary(activity, mostDenied, malformed);
    }

    private static Entry? TryParse(string line)
    {
        var parts = line.Split(',');
        if (parts.Length != 5)
            return null;

        var (timestamp, user, path, decision, response) = (parts[0].Trim(), parts[1].Trim(), parts[2].Trim(), parts[3].Trim(), parts[4].Trim());

        // A timestamp without a zone is ambiguous; refuse it rather than guess.
        if (!timestamp.EndsWith('Z') && !HasOffset(timestamp))
            return null;

        if (!DateTimeOffset.TryParse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.None, out var at))
            return null;

        if (user.Length == 0 || !path.StartsWith('/'))
            return null;

        bool allowed;
        if (decision.Equals("ALLOW", StringComparison.OrdinalIgnoreCase)) allowed = true;
        else if (decision.Equals("DENY", StringComparison.OrdinalIgnoreCase)) allowed = false;
        else return null;

        // NumberStyles.None: no sign, no spaces, no thousands separators: "-5" and "1,000" are malformed.
        if (!int.TryParse(response, NumberStyles.None, CultureInfo.InvariantCulture, out var ms))
            return null;

        return new Entry(at, user.ToLowerInvariant(), path, allowed, ms);
    }

    private static bool HasOffset(string timestamp) =>
        timestamp.Length > 6 && (timestamp[^6] == '+' || timestamp[^6] == '-') && timestamp[^3] == ':';
}
