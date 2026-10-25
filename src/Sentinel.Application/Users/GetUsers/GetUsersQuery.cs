using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Application.Abstractions.Validation;
using Sentinel.Contracts;

namespace Sentinel.Application.Users.GetUsers;

/// <summary>
/// The user register. <c>Search</c> matches part of a display name, or a
/// whole email address.
/// </summary>
public sealed record GetUsersQuery(string? Search, int Page = 1, int PageSize = 25) : IQuery<PagedResponse<UserSummaryDto>>;

internal sealed class GetUsersQueryValidator : Validator<GetUsersQuery>
{
    public override IEnumerable<ValidationFailure> Validate(GetUsersQuery request) =>
        InRange(request.Page, 1, int.MaxValue, nameof(request.Page))
            .Concat(InRange(request.PageSize, 1, 100, nameof(request.PageSize)))
            .Concat(MaxLength(request.Search, 100, nameof(request.Search)));
}
