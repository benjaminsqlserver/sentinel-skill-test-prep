namespace Sentinel.Domain.Security;

public enum FindingKind
{
    BruteForce,
    ImpossibleTravel,
    NewCountry,
    NewDevice
}

/// <summary>Ordered: a higher value is more serious, so <c>Max()</c> is meaningful.</summary>
public enum Severity
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3
}

/// <summary>
/// One reason to think the account may be compromised.
/// </summary>
/// <param name="Key">
/// Stable across recomputation (it is built from attempt ids, not from a
/// counter), so the user can acknowledge it and the acknowledgement sticks.
/// </param>
/// <param name="OccurredAtUtc">When the suspicious attempt happened (the latest one, for a burst).</param>
/// <param name="AttemptIds">The sign-in attempts that are the evidence for this finding.</param>
public sealed record SecurityFinding(
    string Key,
    FindingKind Kind,
    Severity Severity,
    DateTime OccurredAtUtc,
    IReadOnlyList<Guid> AttemptIds,
    string Summary);

/// <summary>The verdict for an account: the findings, and the worst of them.</summary>
public sealed record SecurityAssessment(Severity RiskLevel, IReadOnlyList<SecurityFinding> Findings)
{
    public static SecurityAssessment Clear { get; } = new(Severity.None, []);

    /// <summary>Medium or above: worth interrupting the user about.</summary>
    public bool IsCompromiseSuspected => RiskLevel >= Severity.Medium;
}
