using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Application.Abstractions.Validation;
using Sentinel.Domain.Security;

namespace Sentinel.Application.Security.AcknowledgeFinding;

/// <summary>"That was me." Idempotent: acknowledging twice is not an error.</summary>
public sealed record AcknowledgeFindingCommand(Guid UserId, string FindingKey) : ICommand;

internal sealed class AcknowledgeFindingCommandValidator : Validator<AcknowledgeFindingCommand>
{
    public override IEnumerable<ValidationFailure> Validate(AcknowledgeFindingCommand request) =>
        Required(request.FindingKey, nameof(request.FindingKey))
            .Concat(MaxLength(request.FindingKey, FindingAcknowledgement.MaxKeyLength, nameof(request.FindingKey)));
}
