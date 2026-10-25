using Microsoft.EntityFrameworkCore;
using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Application.Abstractions.Ports;
using Sentinel.Contracts;
using Sentinel.Domain.Common;

namespace Sentinel.Application.Roles.GetRoles;

internal sealed class GetRolesQueryHandler(IApplicationDbContext db) : IQueryHandler<GetRolesQuery, IReadOnlyList<RoleDto>>
{
    public async Task<Result<IReadOnlyList<RoleDto>>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await db.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync(cancellationToken);

        var userCounts = await db.Users
            .AsNoTracking()
            .SelectMany(u => u.Roles)
            .GroupBy(r => r.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count, cancellationToken);

        IReadOnlyList<RoleDto> result = roles
            .Select(r => new RoleDto(
                r.Id,
                r.Name,
                r.Description,
                r.Permissions.Select(p => p.Value).Order(StringComparer.Ordinal).ToList(),
                userCounts.GetValueOrDefault(r.Id)))
            .ToList();

        return Result.Success(result);
    }
}
