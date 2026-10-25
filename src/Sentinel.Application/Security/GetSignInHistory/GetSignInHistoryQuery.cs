using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Application.Abstractions.Validation;
using Sentinel.Contracts;

namespace Sentinel.Application.Security.GetSignInHistory;

/// <summary>Every sign-in for one user, newest first, a page at a time.</summary>
public sealed record GetSignInHistoryQuery(Guid UserId, int Page = 1, int PageSize = 50) : IQuery<PagedResponse<SignInAttemptDto>>;

internal sealed class GetSignInHistoryQueryValidator : Validator<GetSignInHistoryQuery>
{
    public override IEnumerable<ValidationFailure> Validate(GetSignInHistoryQuery request) =>
        InRange(request.Page, 1, int.MaxValue, nameof(request.Page))
            .Concat(InRange(request.PageSize, 1, 200, nameof(request.PageSize)));
}
