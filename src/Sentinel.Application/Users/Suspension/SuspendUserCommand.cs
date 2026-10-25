using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Application.Abstractions.Validation;

namespace Sentinel.Application.Users.Suspension;

public sealed record SuspendUserCommand(Guid UserId, string Reason) : ICommand;

public sealed record ReactivateUserCommand(Guid UserId) : ICommand;

internal sealed class SuspendUserCommandValidator : Validator<SuspendUserCommand>
{
    public override IEnumerable<ValidationFailure> Validate(SuspendUserCommand request) =>
        Required(request.Reason, nameof(request.Reason))
            .Concat(MaxLength(request.Reason, 500, nameof(request.Reason)));
}
