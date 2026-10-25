using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Application.Abstractions.Validation;
using Sentinel.Domain.Access;

namespace Sentinel.Application.Users.PermissionOverrides;

public sealed record SetPermissionOverrideCommand(Guid UserId, string Permission, string Effect) : ICommand;

public sealed record ClearPermissionOverrideCommand(Guid UserId, string Permission) : ICommand;

internal sealed class SetPermissionOverrideCommandValidator : Validator<SetPermissionOverrideCommand>
{
    public override IEnumerable<ValidationFailure> Validate(SetPermissionOverrideCommand request)
    {
        foreach (var failure in Required(request.Permission, nameof(request.Permission)))
            yield return failure;

        // Enum.TryParse accepts "1" and "Allow, Deny"; insist on a defined name.
        if (!Enum.TryParse<PermissionEffect>(request.Effect, ignoreCase: true, out var effect) ||
            !Enum.IsDefined(effect) || int.TryParse(request.Effect, out _))
        {
            yield return new ValidationFailure(nameof(request.Effect), "Effect must be 'Allow' or 'Deny'.");
        }
    }
}
