using Microsoft.EntityFrameworkCore;
using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Application.Abstractions.Ports;
using Sentinel.Domain.Common;
using Sentinel.Domain.Security;
using Sentinel.Domain.Users;

namespace Sentinel.Application.Security.RecordSignIn;

internal sealed class RecordSignInCommandHandler(IApplicationDbContext db, IGeoLocator geoLocator, IClock clock)
    : ICommandHandler<RecordSignInCommand, Guid>
{
    /// <summary>Clocks disagree. Allow a little skew, but not "next Tuesday".</summary>
    public static readonly TimeSpan MaxClockSkew = TimeSpan.FromMinutes(5);

    public async Task<Result<Guid>> Handle(RecordSignInCommand request, CancellationToken cancellationToken)
    {
        var email = EmailAddress.Create(request.Email);
        if (email.IsFailure)
            return email.Error!;

        var occurredAtUtc = request.OccurredAt.UtcDateTime; // Kind is Utc by definition
        if (occurredAtUtc > clock.UtcNow + MaxClockSkew)
            return Error.Validation("SignIn.InFuture", "A sign-in cannot be recorded in the future.");

        var userId = await db.Users
            .Where(u => u.Email == email.Value)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (userId is null)
            return Error.NotFound("User.EmailNotFound", $"No user has the email '{email.Value}'.");

        SignInFailureReason? reason = request.FailureReason is null
            ? null
            : Enum.Parse<SignInFailureReason>(request.FailureReason, ignoreCase: true);

        var attempt = SignInAttempt.Record(
            userId.Value,
            occurredAtUtc,
            request.IpAddress,
            geoLocator.Locate(request.IpAddress),
            request.UserAgent,
            request.Succeeded,
            reason);

        if (attempt.IsFailure)
            return attempt.Error!;

        db.SignInAttempts.Add(attempt.Value);
        await db.SaveChangesAsync(cancellationToken);
        return attempt.Value.Id;
    }
}
