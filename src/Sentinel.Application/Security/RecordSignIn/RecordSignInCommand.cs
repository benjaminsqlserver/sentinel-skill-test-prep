using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Application.Abstractions.Validation;
using Sentinel.Domain.Security;

namespace Sentinel.Application.Security.RecordSignIn;

/// <summary>
/// Records one sign-in attempt. In production the identity provider calls
/// this; in development the seeder and the "simulate" buttons do.
/// <c>OccurredAt</c> is a <see cref="DateTimeOffset"/> on purpose: a bare
/// DateTime from the wire cannot say which zone it is in.
/// </summary>
public sealed record RecordSignInCommand(
    string Email,
    DateTimeOffset OccurredAt,
    string IpAddress,
    string? UserAgent,
    bool Succeeded,
    string? FailureReason) : ICommand<Guid>;

internal sealed class RecordSignInCommandValidator : Validator<RecordSignInCommand>
{
    public override IEnumerable<ValidationFailure> Validate(RecordSignInCommand request)
    {
        foreach (var failure in Required(request.Email, nameof(request.Email)).Concat(Required(request.IpAddress, nameof(request.IpAddress))))
            yield return failure;

        if (request.FailureReason is not null && !Enum.TryParse<SignInFailureReason>(request.FailureReason, ignoreCase: true, out _))
        {
            yield return new ValidationFailure(nameof(request.FailureReason),
                $"FailureReason must be one of: {string.Join(", ", Enum.GetNames<SignInFailureReason>())}.");
        }

        if (request.OccurredAt == default)
            yield return new ValidationFailure(nameof(request.OccurredAt), "OccurredAt is required.");
    }
}
