using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Application.Abstractions.Validation;
using Sentinel.Domain.Pages;

namespace Sentinel.Application.Pages.CreatePage;

/// <summary>
/// Adds a page to the tree. The parent is given by path rather than id
/// because that is how an administrator thinks about the tree.
/// </summary>
public sealed record CreatePageCommand(
    string Path,
    string Title,
    string? ParentPath,
    string? RequiredPermission,
    bool IsPublic,
    int SortOrder) : ICommand<Guid>;

internal sealed class CreatePageCommandValidator : Validator<CreatePageCommand>
{
    public override IEnumerable<ValidationFailure> Validate(CreatePageCommand request) =>
        Required(request.Path, nameof(request.Path))
            .Concat(MaxLength(request.Path, 200, nameof(request.Path)))
            .Concat(Required(request.Title, nameof(request.Title)))
            .Concat(MaxLength(request.Title, Page.MaxTitleLength, nameof(request.Title)))
            .Concat(InRange(request.SortOrder, 0, 10_000, nameof(request.SortOrder)));
}
