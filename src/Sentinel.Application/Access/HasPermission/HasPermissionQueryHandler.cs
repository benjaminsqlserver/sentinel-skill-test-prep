using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Application.Abstractions.Ports;
using Sentinel.Domain.Access;
using Sentinel.Domain.Common;
using Sentinel.Domain.Users;

namespace Sentinel.Application.Access.HasPermission;

internal sealed class HasPermissionQueryHandler(IApplicationDbContext db) : IQueryHandler<HasPermissionQuery, bool>
{
    public async Task<Result<bool>> Handle(HasPermissionQuery request, CancellationToken cancellationToken)
    {
        var permission = Permission.Create(request.Permission);
        if (permission.IsFailure)
            return permission.Error!;

        var subject = await db.LoadAccessSubjectAsync(request.UserId, cancellationToken);
        if (subject is null)
            return UserErrors.NotFound(request.UserId);

        return subject.Has(permission.Value);
    }
}
