using Microsoft.EntityFrameworkCore;
using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Application.Abstractions.Ports;
using Sentinel.Domain.Access;
using Sentinel.Domain.Common;
using Sentinel.Domain.Users;

namespace Sentinel.Application.Users.PermissionOverrides;

internal sealed class SetPermissionOverrideCommandHandler(IApplicationDbContext db) : ICommandHandler<SetPermissionOverrideCommand>
{
    public async Task<Result> Handle(SetPermissionOverrideCommand request, CancellationToken cancellationToken)
    {
        var permission = Permission.Create(request.Permission);
        if (permission.IsFailure)
            return permission.Error!;

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user is null)
            return UserErrors.NotFound(request.UserId);

        // The validator has already proved this parses.
        var effect = Enum.Parse<PermissionEffect>(request.Effect, ignoreCase: true);

        var result = user.SetOverride(permission.Value, effect);
        if (result.IsFailure)
            return result;

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

internal sealed class ClearPermissionOverrideCommandHandler(IApplicationDbContext db) : ICommandHandler<ClearPermissionOverrideCommand>
{
    public async Task<Result> Handle(ClearPermissionOverrideCommand request, CancellationToken cancellationToken)
    {
        var permission = Permission.Create(request.Permission);
        if (permission.IsFailure)
            return permission.Error!;

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user is null)
            return UserErrors.NotFound(request.UserId);

        var result = user.ClearOverride(permission.Value);
        if (result.IsFailure)
            return result;

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
