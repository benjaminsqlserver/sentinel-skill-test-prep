using Microsoft.EntityFrameworkCore;
using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Application.Abstractions.Ports;
using Sentinel.Domain.Common;
using Sentinel.Domain.Users;

namespace Sentinel.Application.Users.CreateUser;

internal sealed class CreateUserCommandHandler(IApplicationDbContext db, IClock clock)
    : ICommandHandler<CreateUserCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var email = EmailAddress.Create(request.Email);
        if (email.IsFailure)
            return email.Error!;

        // The unique index is the real guarantee; this check exists to turn
        // the common case into a friendly 409 instead of a database exception.
        if (await db.Users.AnyAsync(u => u.Email == email.Value, cancellationToken))
            return UserErrors.DuplicateEmail(email.Value);

        var user = User.Create(email.Value, request.DisplayName, request.TimeZoneId, clock.UtcNow);
        if (user.IsFailure)
            return user.Error!;

        db.Users.Add(user.Value);
        await db.SaveChangesAsync(cancellationToken);

        return user.Value.Id;
    }
}
