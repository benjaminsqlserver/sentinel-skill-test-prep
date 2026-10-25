using Microsoft.EntityFrameworkCore;
using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Application.Abstractions.Ports;
using Sentinel.Domain.Common;
using Sentinel.Domain.Security;

namespace Sentinel.Application.Security.AcknowledgeFinding;

internal sealed class AcknowledgeFindingCommandHandler(IApplicationDbContext db, AccountAssessor assessor, IClock clock)
    : ICommandHandler<AcknowledgeFindingCommand>
{
    public async Task<Result> Handle(AcknowledgeFindingCommand request, CancellationToken cancellationToken)
    {
        var alreadyAcknowledged = await db.FindingAcknowledgements
            .AnyAsync(a => a.UserId == request.UserId && a.FindingKey == request.FindingKey, cancellationToken);

        if (alreadyAcknowledged)
            return Result.Success();

        // Only findings the user can currently see may be acknowledged. This
        // stops a client from pre-acknowledging keys it has guessed, which
        // would let a compromised session hide its own tracks in advance.
        var current = await assessor.AssessAsync(request.UserId, cancellationToken);
        if (current.Assessment.Findings.All(f => f.Key != request.FindingKey))
            return Error.NotFound("Finding.NotFound", "That finding does not exist or no longer applies.");

        var acknowledgement = FindingAcknowledgement.Create(request.UserId, request.FindingKey, clock.UtcNow);
        if (acknowledgement.IsFailure)
            return acknowledgement.Error!;

        db.FindingAcknowledgements.Add(acknowledgement.Value);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
