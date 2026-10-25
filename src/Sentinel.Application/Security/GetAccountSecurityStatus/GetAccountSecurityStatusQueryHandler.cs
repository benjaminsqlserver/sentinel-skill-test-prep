using Microsoft.EntityFrameworkCore;
using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Application.Abstractions.Ports;
using Sentinel.Contracts;
using Sentinel.Domain.Common;
using Sentinel.Domain.Users;

namespace Sentinel.Application.Security.GetAccountSecurityStatus;

internal sealed class GetAccountSecurityStatusQueryHandler(IApplicationDbContext db, AccountAssessor assessor)
    : IQueryHandler<GetAccountSecurityStatusQuery, AccountSecurityStatusDto>
{
    /// <summary>How many sign-ins the page shows under the banner. The full list is paged separately.</summary>
    public const int RecentSignInCount = 20;

    public async Task<Result<AccountSecurityStatusDto>> Handle(GetAccountSecurityStatusQuery request, CancellationToken cancellationToken)
    {
        var timeZoneId = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == request.UserId)
            .Select(u => u.TimeZoneId)
            .FirstOrDefaultAsync(cancellationToken);

        if (timeZoneId is null)
            return UserErrors.NotFound(request.UserId);

        var result = await assessor.AssessAsync(request.UserId, cancellationToken);

        var recent = result.History
            .OrderByDescending(a => a.OccurredAtUtc)
            .Take(RecentSignInCount)
            .Select(a => a.ToDto(result.FlaggedAttemptIds))
            .ToList();

        return new AccountSecurityStatusDto(
            result.Assessment.RiskLevel.ToString(),
            result.Assessment.IsCompromiseSuspected,
            result.AssessedAtUtc,
            timeZoneId,
            result.Assessment.Findings.Select(f => f.ToDto()).ToList(),
            recent);
    }
}
