using Microsoft.EntityFrameworkCore;
using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Application.Abstractions.Ports;
using Sentinel.Contracts;
using Sentinel.Domain.Common;
using Sentinel.Domain.Users;

namespace Sentinel.Application.Users.GetUserById;

internal sealed class GetUserByIdQueryHandler(IApplicationDbContext db) : IQueryHandler<GetUserByIdQuery, UserDetailDto>
{
    public async Task<Result<UserDetailDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
            return UserErrors.NotFound(request.UserId);

        var roleIds = user.RoleIds.ToList();
        var roles = await db.Roles
            .AsNoTracking()
            .Where(r => roleIds.Contains(r.Id))
            .OrderBy(r => r.Name)
            .Select(r => new RoleSummaryDto(r.Id, r.Name))
            .ToListAsync(cancellationToken);

        return new UserDetailDto(
            user.Id,
            user.Email.Value,
            user.DisplayName,
            user.TimeZoneId,
            user.Status.ToString(),
            user.SuspensionReason,
            user.CreatedAtUtc,
            roles,
            user.Overrides
                .OrderBy(o => o.Permission.Value)
                .Select(o => new PermissionOverrideDto(o.Permission.Value, o.Effect.ToString()))
                .ToList());
    }
}
