using Microsoft.EntityFrameworkCore;
using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Application.Abstractions.Ports;
using Sentinel.Domain.Access;
using Sentinel.Domain.Common;
using Sentinel.Domain.Roles;

namespace Sentinel.Application.Roles.RolePermissions;

internal sealed class GrantPermissionCommandHandler(IApplicationDbContext db) : ICommandHandler<GrantPermissionCommand>
{
    public async Task<Result> Handle(GrantPermissionCommand request, CancellationToken cancellationToken)
    {
        var permission = Permission.Create(request.Permission);
        if (permission.IsFailure)
            return permission.Error!;

        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);
        if (role is null)
            return RoleErrors.NotFound(request.RoleId);

        var result = role.Grant(permission.Value);
        if (result.IsFailure)
            return result;

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

internal sealed class RevokePermissionCommandHandler(IApplicationDbContext db) : ICommandHandler<RevokePermissionCommand>
{
    public async Task<Result> Handle(RevokePermissionCommand request, CancellationToken cancellationToken)
    {
        var permission = Permission.Create(request.Permission);
        if (permission.IsFailure)
            return permission.Error!;

        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);
        if (role is null)
            return RoleErrors.NotFound(request.RoleId);

        var result = role.Revoke(permission.Value);
        if (result.IsFailure)
            return result;

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
