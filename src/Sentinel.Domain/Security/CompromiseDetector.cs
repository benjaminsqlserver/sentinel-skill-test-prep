namespace Sentinel.Domain.Security;

/// <summary>
/// Looks at one user's sign-in history and says whether the account may be
/// compromised. Four rules, each independent:
/// <list type="bullet">
///   <item><b>Brute force</b>: at least N failures inside a short window. High if a success follows the burst.</item>
///   <item><b>Impossible travel</b>: two successful sign-ins further apart than anyone could travel in the time between them. High.</item>
///   <item><b>New country</b>: a successful sign-in from a country this user has never signed in from. Medium.</item>
///   <item><b>New device</b>: a successful sign-in from a device this user has never used. Low.</item>
/// </list>
/// </summary>
/// <remarks>
/// <para>
/// The history passed in is both the evidence and the baseline. Attempts older
/// than the reporting window never produce findings, but they still count as
/// "seen before" for the new-country and new-device rules. Callers should
/// therefore pass more history than the reporting window: the application
/// layer passes ninety days against a thirty-day window.
/// </para>
/// <para>
/// The detector sorts its input itself and does not trust the caller's order.
/// It cannot, however, notice that the caller sent the wrong <i>subset</i> of
/// history; Chapter 23 is about exactly that.
/// </para>
/// </remarks>
public static class CompromiseDetector
{
    public static SecurityAssessment Assess(
        IEnumerable<SignInAttempt> history,
        DateTime nowUtc,
        IReadOnlySet<string>? acknowledgedKeys = null,
        DetectionPolicy? policy = null)
    {
        ArgumentNullException.ThrowIfNull(history);
        if (nowUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("nowUtc must be UTC.", nameof(nowUtc));

        policy ??= DetectionPolicy.Default;
        acknowledgedKeys ??= new HashSet<string>();

        // Oldest first; ties broken by id so the output is deterministic.
        // Attempts "from the future" (clock skew upstream) are ignored rather
        // than allowed to poison the baseline.
        var ordered = history
            .Where(a => a.OccurredAtUtc <= nowUtc)
            .OrderBy(a => a.OccurredAtUtc)
            .ThenBy(a => a.Id)
            .ToList();

        if (ordered.Count == 0)
            return SecurityAssessment.Clear;

        var reportFrom = nowUtc - policy.ReportingWindow;

        var findings = DetectBruteForce(ordered, policy)
            .Concat(DetectImpossibleTravel(ordered, policy))
            .Concat(DetectNewCountries(ordered))
            .Concat(DetectNewDevices(ordered))
            .Where(f => f.OccurredAtUtc >= reportFrom)
            .Where(f => !acknowledgedKeys.Contains(f.Key))
            .OrderByDescending(f => f.Severity)
            .ThenByDescending(f => f.OccurredAtUtc)
            .ToList();

        var riskLevel = findings.Count == 0 ? Severity.None : findings.Max(f => f.Severity);
        return new SecurityAssessment(riskLevel, findings);
    }

    /// <summary>
    /// Groups failures into bursts (a gap longer than the window ends a burst),
    /// then reports a burst when some window-sized slice of it holds at least
    /// the threshold number of failures.
    /// </summary>
    private static IEnumerable<SecurityFinding> DetectBruteForce(List<SignInAttempt> ordered, DetectionPolicy policy)
    {
        var failures = ordered.Where(a => !a.Succeeded).ToList();
        var burst = new List<SignInAttempt>();

        foreach (var failure in failures)
        {
            if (burst.Count > 0 && failure.OccurredAtUtc - burst[^1].OccurredAtUtc > policy.FailedAttemptWindow)
            {
                if (TryReportBurst(burst, ordered, policy) is { } finding)
                    yield return finding;
                burst = [];
            }

            burst.Add(failure);
        }

        if (TryReportBurst(burst, ordered, policy) is { } last)
            yield return last;
    }

    private static SecurityFinding? TryReportBurst(List<SignInAttempt> burst, List<SignInAttempt> ordered, DetectionPolicy policy)
    {
        if (burst.Count < policy.FailedAttemptThreshold || MaxInAnyWindow(burst, policy.FailedAttemptWindow) < policy.FailedAttemptThreshold)
            return null;

        var first = burst[0];
        var last = burst[^1];

        // Did the attacker get in? A success shortly after the burst is the
        // difference between "someone is trying" and "someone succeeded".
        var successAfter = ordered.FirstOrDefault(a =>
            a.Succeeded &&
            a.OccurredAtUtc > first.OccurredAtUtc &&
            a.OccurredAtUtc <= last.OccurredAtUtc + policy.FailedAttemptWindow);

        var evidence = burst.Select(a => a.Id).ToList();
        if (successAfter is not null)
            evidence.Add(successAfter.Id);

        var countries = burst.Select(a => a.Location).Where(l => l.IsKnown).Select(l => l.CountryCode).Distinct().ToList();
        var from = countries.Count == 0 ? "an unknown location" : string.Join(", ", countries);

        return new SecurityFinding(
            Key: $"brute-force:{first.Id:N}",
            Kind: FindingKind.BruteForce,
            Severity: successAfter is null ? Severity.Medium : Severity.High,
            OccurredAtUtc: successAfter?.OccurredAtUtc ?? last.OccurredAtUtc,
            AttemptIds: evidence,
            Summary: successAfter is null
                ? $"{burst.Count} failed sign-in attempts from {from} in a short period."
                : $"{burst.Count} failed sign-in attempts from {from}, followed by a successful sign-in.");
    }

    /// <summary>Two-pointer sweep: the most failures that fit inside any one window.</summary>
    private static int MaxInAnyWindow(List<SignInAttempt> burst, TimeSpan window)
    {
        var best = 0;
        var start = 0;
        for (var end = 0; end < burst.Count; end++)
        {
            while (burst[end].OccurredAtUtc - burst[start].OccurredAtUtc > window)
                start++;
            best = Math.Max(best, end - start + 1);
        }
        return best;
    }

    private static IEnumerable<SecurityFinding> DetectImpossibleTravel(List<SignInAttempt> ordered, DetectionPolicy policy)
    {
        SignInAttempt? previous = null;

        foreach (var current in ordered.Where(a => a.Succeeded && a.Location.HasCoordinates))
        {
            if (previous is not null && previous.Location.DistanceKmTo(current.Location) is { } km && km >= policy.MinTravelDistanceKm)
            {
                var hours = (current.OccurredAtUtc - previous.OccurredAtUtc).TotalHours;

                // Zero elapsed time over 500 km is the clearest case of all;
                // guard the division rather than letting it produce infinity.
                var speedKmh = hours <= 0 ? double.PositiveInfinity : km / hours;

                if (speedKmh > policy.MaxPlausibleSpeedKmh)
                {
                    yield return new SecurityFinding(
                        Key: $"impossible-travel:{current.Id:N}",
                        Kind: FindingKind.ImpossibleTravel,
                        Severity: Severity.High,
                        OccurredAtUtc: current.OccurredAtUtc,
                        AttemptIds: [previous.Id, current.Id],
                        Summary: $"Signed in from {previous.Location} and then {current.Location} " +
                                 $"{FormatElapsed(current.OccurredAtUtc - previous.OccurredAtUtc)} later ({km:N0} km apart).");
                }
            }

            previous = current;
        }
    }

    private static IEnumerable<SecurityFinding> DetectNewCountries(List<SignInAttempt> ordered)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var isFirstSuccess = true;

        foreach (var attempt in ordered.Where(a => a.Succeeded && a.Location.IsKnown))
        {
            // The very first success sets the baseline. Without history there
            // is nothing to compare against, and flagging every new account
            // as "new country" would train users to ignore the banner.
            if (seen.Add(attempt.Location.CountryCode) && !isFirstSuccess)
            {
                yield return new SecurityFinding(
                    Key: $"new-country:{attempt.Id:N}",
                    Kind: FindingKind.NewCountry,
                    Severity: Severity.Medium,
                    OccurredAtUtc: attempt.OccurredAtUtc,
                    AttemptIds: [attempt.Id],
                    Summary: $"First sign-in from {attempt.Location}.");
            }

            isFirstSuccess = false;
        }
    }

    private static IEnumerable<SecurityFinding> DetectNewDevices(List<SignInAttempt> ordered)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var isFirstSuccess = true;

        foreach (var attempt in ordered.Where(a => a.Succeeded))
        {
            if (seen.Add(attempt.DeviceId) && !isFirstSuccess)
            {
                yield return new SecurityFinding(
                    Key: $"new-device:{attempt.Id:N}",
                    Kind: FindingKind.NewDevice,
                    Severity: Severity.Low,
                    OccurredAtUtc: attempt.OccurredAtUtc,
                    AttemptIds: [attempt.Id],
                    Summary: $"Sign-in from a device not seen before ({DeviceFingerprint.Describe(attempt.UserAgent)}).");
            }

            isFirstSuccess = false;
        }
    }

    private static string FormatElapsed(TimeSpan elapsed) => elapsed.TotalMinutes < 60
        ? $"{Math.Max(0, (int)elapsed.TotalMinutes)} minutes"
        : $"{elapsed.TotalHours:0.#} hours";
}
