using Sentinel.Contracts;
using Sentinel.Domain.Security;

namespace Sentinel.Application.Security;

internal static class SecurityMapping
{
    public static SignInAttemptDto ToDto(this SignInAttempt attempt, IReadOnlySet<Guid> flaggedIds) => new(
        attempt.Id,
        attempt.OccurredAtUtc,
        attempt.IpAddress,
        attempt.Location.CountryCode,
        attempt.Location.City,
        DeviceFingerprint.Describe(attempt.UserAgent),
        attempt.Succeeded,
        attempt.FailureReason?.ToString(),
        flaggedIds.Contains(attempt.Id));

    public static SecurityFindingDto ToDto(this SecurityFinding finding) => new(
        finding.Key,
        finding.Kind.ToString(),
        finding.Severity.ToString(),
        finding.OccurredAtUtc,
        finding.Summary,
        finding.AttemptIds);
}
