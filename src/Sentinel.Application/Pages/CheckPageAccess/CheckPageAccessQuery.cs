using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Application.Abstractions.Validation;
using Sentinel.Contracts;

namespace Sentinel.Application.Pages.CheckPageAccess;

/// <summary>
/// May this user open this one page? The Angular route guard asks this
/// before every navigation, so a hand-typed URL is checked as carefully as
/// a click on the menu.
/// </summary>
public sealed record CheckPageAccessQuery(Guid? UserId, string Path) : IQuery<PageAccessDto>;

internal sealed class CheckPageAccessQueryValidator : Validator<CheckPageAccessQuery>
{
    public override IEnumerable<ValidationFailure> Validate(CheckPageAccessQuery request) =>
        Required(request.Path, nameof(request.Path))
            .Concat(MaxLength(request.Path, 200, nameof(request.Path)));
}
