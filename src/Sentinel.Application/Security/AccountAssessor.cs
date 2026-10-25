using Microsoft.EntityFrameworkCore;
using Sentinel.Application.Abstractions.Ports;
using Sentinel.Domain.Security;

namespace Sentinel.Application.Security;

internal sealed record AccountAssessment(
    SecurityAssessment Assessment,
    IReadOnlyList<SignInAttempt> History,
    DateTime AssessedAtUtc)
{
    /// <summary>Ids of every attempt cited as evidence by an unacknowledged finding.</summary>
    public IReadOnlySet<Guid> FlaggedAttemptIds { get; } =
        Assessment.Findings.SelectMany(f => f.AttemptIds).ToHashSet();
}

/// <summary>
/// Loads a user's history and runs the detector over it. Shared by the
/// status query, the history query and the acknowledge command, because all
/// three must agree on what the findings are: a user cannot acknowledge a
/// finding the status page did not show them.
/// </summary>
internal sealed class AccountAssessor(IApplicationDbContext db, IClock clock, DetectionPolicy policy)
{
    /// <summary>History older than the reporting window, used as the baseline for "new" countries and devices.</summary>
    public static readonly TimeSpan BaselineWindow = TimeSpan.FromDays(90);

    /// <summary>
    /// A ceiling on how much history one assessment loads. An integration
    /// account that signs in every minute must not turn the security page
    /// into a full table scan.
    /// </summary>
    public const int MaxHistory = 500;

    public async Task<AccountAssessment> AssessAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var since = now - BaselineWindow;

        // Chronological order: the detector walks history oldest-first, so
        // let the database sort it rather than sorting again in memory.
        var history = await db.SignInAttempts
            .AsNoTracking()
            .Where(a => a.UserId == userId && a.OccurredAtUtc >= since)
            .OrderBy(a => a.OccurredAtUtc)
            .Take(MaxHistory)
            .ToListAsync(cancellationToken);

        var acknowledged = await db.FindingAcknowledgements
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .Select(a => a.FindingKey)
            .ToListAsync(cancellationToken);

        var assessment = CompromiseDetector.Assess(history, now, acknowledged.ToHashSet(StringComparer.Ordinal), policy);
        return new AccountAssessment(assessment, history, now);
    }
}
