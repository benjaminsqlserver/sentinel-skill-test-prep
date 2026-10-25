using Microsoft.EntityFrameworkCore;
using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Application.Abstractions.Ports;
using Sentinel.Contracts;
using Sentinel.Domain.Common;
using Sentinel.Domain.Users;

namespace Sentinel.Application.Security.GetSignInHistory;

internal sealed class GetSignInHistoryQueryHandler(IApplicationDbContext db, AccountAssessor assessor)
    : IQueryHandler<GetSignInHistoryQuery, PagedResponse<SignInAttemptDto>>
{
    public async Task<Result<PagedResponse<SignInAttemptDto>>> Handle(GetSignInHistoryQuery request, CancellationToken cancellationToken)
    {
        if (!await db.Users.AnyAsync(u => u.Id == request.UserId, cancellationToken))
            return UserErrors.NotFound(request.UserId);

        var attempts = db.SignInAttempts.AsNoTracking().Where(a => a.UserId == request.UserId);
        var total = await attempts.CountAsync(cancellationToken);

        var page = await attempts
            .OrderByDescending(a => a.OccurredAtUtc)
            .ThenByDescending(a => a.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // Flags come from the same assessment the status page uses, so a row
        // highlighted here is always explained by a finding there.
        var flagged = (await assessor.AssessAsync(request.UserId, cancellationToken)).FlaggedAttemptIds;

        return new PagedResponse<SignInAttemptDto>(
            page.Select(a => a.ToDto(flagged)).ToList(), request.Page, request.PageSize, total);
    }
}
