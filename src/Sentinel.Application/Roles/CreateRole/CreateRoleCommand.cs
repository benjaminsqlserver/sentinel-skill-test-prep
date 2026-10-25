using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Application.Abstractions.Validation;
using Sentinel.Domain.Roles;

namespace Sentinel.Application.Roles.CreateRole;

public sealed record CreateRoleCommand(string Name, string? Description) : ICommand<Guid>;

internal sealed class CreateRoleCommandValidator : Validator<CreateRoleCommand>
{
    public override IEnumerable<ValidationFailure> Validate(CreateRoleCommand request) =>
        Required(request.Name, nameof(request.Name))
            .Concat(MaxLength(request.Name, Role.MaxNameLength, nameof(request.Name)))
            .Concat(MaxLength(request.Description, 500, nameof(request.Description)));
}
