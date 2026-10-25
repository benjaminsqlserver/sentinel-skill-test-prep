using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Application.Abstractions.Validation;
using Sentinel.Domain.Users;

namespace Sentinel.Application.Users.CreateUser;

public sealed record CreateUserCommand(string Email, string DisplayName, string TimeZoneId) : ICommand<Guid>;

internal sealed class CreateUserCommandValidator : Validator<CreateUserCommand>
{
    public override IEnumerable<ValidationFailure> Validate(CreateUserCommand request) =>
        Required(request.Email, nameof(request.Email))
            .Concat(MaxLength(request.Email, EmailAddress.MaxLength, nameof(request.Email)))
            .Concat(Required(request.DisplayName, nameof(request.DisplayName)))
            .Concat(MaxLength(request.DisplayName, User.MaxDisplayNameLength, nameof(request.DisplayName)))
            .Concat(Required(request.TimeZoneId, nameof(request.TimeZoneId)));
}
