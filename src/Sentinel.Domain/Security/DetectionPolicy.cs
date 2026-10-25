namespace Sentinel.Domain.Security;

/// <summary>
/// The thresholds the detector uses. They are data, not constants buried in
/// the rules, because the first thing a security team asks after launch is
/// "can we make it less noisy?", and the answer should be a config change.
/// </summary>
/// <param name="ReportingWindow">Only attempts newer than this produce findings.</param>
/// <param name="FailedAttemptThreshold">Failures within <paramref name="FailedAttemptWindow"/> that count as a brute-force burst.</param>
/// <param name="FailedAttemptWindow">The sliding window for counting failures.</param>
/// <param name="MaxPlausibleSpeedKmh">Faster than this between two sign-ins is "impossible travel". Airliners cruise near 900 km/h.</param>
/// <param name="MinTravelDistanceKm">Ignore jumps shorter than this; IP geolocation is too coarse to judge them.</param>
public sealed record DetectionPolicy(
    TimeSpan ReportingWindow,
    int FailedAttemptThreshold,
    TimeSpan FailedAttemptWindow,
    double MaxPlausibleSpeedKmh,
    double MinTravelDistanceKm)
{
    public static DetectionPolicy Default { get; } = new(
        ReportingWindow: TimeSpan.FromDays(30),
        FailedAttemptThreshold: 5,
        FailedAttemptWindow: TimeSpan.FromMinutes(15),
        MaxPlausibleSpeedKmh: 900,
        MinTravelDistanceKm: 500);
}
