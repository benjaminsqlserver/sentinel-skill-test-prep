namespace Sentinel.Contracts;

/// <summary>
/// A sign-in as the user sees it. <c>OccurredAtUtc</c> is serialised as
/// ISO 8601 with a trailing <c>Z</c>. The client converts it to the user's
/// zone for display; it must never be shown as-is or parsed as local time.
/// </summary>
public sealed record SignInAttemptDto(
    Guid Id,
    DateTime OccurredAtUtc,
    string IpAddress,
    string CountryCode,
    string City,
    string Device,
    bool Succeeded,
    string? FailureReason,
    bool IsFlagged);

public sealed record SecurityFindingDto(
    string Key,
    string Kind,
    string Severity,
    DateTime OccurredAtUtc,
    string Summary,
    IReadOnlyList<Guid> AttemptIds);

/// <summary>
/// Everything the account security page needs in one response, so the page
/// renders from a single round trip and cannot show a banner that disagrees
/// with the table beneath it.
/// </summary>
public sealed record AccountSecurityStatusDto(
    string RiskLevel,
    bool IsCompromiseSuspected,
    DateTime AssessedAtUtc,
    string TimeZoneId,
    IReadOnlyList<SecurityFindingDto> Findings,
    IReadOnlyList<SignInAttemptDto> RecentSignIns);

/// <summary>
/// Posted by the identity provider (simulated in development) after every
/// sign-in attempt. <c>OccurredAtUtc</c> must carry a <c>Z</c> or an offset.
/// </summary>
public sealed record RecordSignInRequest(
    string Email,
    DateTimeOffset OccurredAt,
    string IpAddress,
    string? UserAgent,
    bool Succeeded,
    string? FailureReason);
