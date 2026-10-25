using Microsoft.EntityFrameworkCore;
using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Application.Abstractions.Ports;
using Sentinel.Domain.Common;
using Sentinel.Domain.Users;

namespace Sentinel.Application.Users.Suspension;

internal sealed class SuspendUserCommandHandler(IApplicationDbContext db) : ICommandHandler<SuspendUserCommand>
{
    public async Task<Result> Handle(SuspendUserCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user is null)
            return UserErrors.NotFound(request.UserId);

        var result = user.Suspend(request.Reason);
        if (result.IsFailure)
            return result;

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

internal sealed class ReactivateUserCommandHandler(IApplicationDbContext db) : ICommandHandler<ReactivateUserCommand>
{
    public async Task<Result> Handle(ReactivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user is null)
            return UserErrors.NotFound(request.UserId);

        var result = user.Reactivate();
        if (result.IsFailure)
            return result;

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
