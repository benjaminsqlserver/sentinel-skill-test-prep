using Microsoft.EntityFrameworkCore;
using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Application.Abstractions.Ports;
using Sentinel.Domain.Common;
using Sentinel.Domain.Roles;
using Sentinel.Domain.Users;

namespace Sentinel.Application.Users.RoleAssignment;

// Assign and revoke are two halves of one use case and change for the same
// reasons, so they share a slice. Two handlers, still one responsibility each.

internal sealed class AssignRoleCommandHandler(IApplicationDbContext db, IClock clock) : ICommandHandler<AssignRoleCommand>
{
    public async Task<Result> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user is null)
            return UserErrors.NotFound(request.UserId);

        if (!await db.Roles.AnyAsync(r => r.Id == request.RoleId, cancellationToken))
            return RoleErrors.NotFound(request.RoleId);

        var result = user.AssignRole(request.RoleId, clock.UtcNow);
        if (result.IsFailure)
            return result;

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

internal sealed class RevokeRoleCommandHandler(IApplicationDbContext db) : ICommandHandler<RevokeRoleCommand>
{
    public async Task<Result> Handle(RevokeRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user is null)
            return UserErrors.NotFound(request.UserId);

        var result = user.RevokeRole(request.RoleId);
        if (result.IsFailure)
            return result;

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
