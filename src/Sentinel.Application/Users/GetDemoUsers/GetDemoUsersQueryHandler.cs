using Microsoft.EntityFrameworkCore;
using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Application.Abstractions.Ports;
using Sentinel.Contracts;
using Sentinel.Domain.Common;
using Sentinel.Domain.Users;

namespace Sentinel.Application.Users.GetDemoUsers;

internal sealed class GetDemoUsersQueryHandler(IApplicationDbContext db) : IQueryHandler<GetDemoUsersQuery, IReadOnlyList<DemoUserDto>>
{
    public async Task<Result<IReadOnlyList<DemoUserDto>>> Handle(GetDemoUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await db.Users.AsNoTracking().OrderBy(u => u.DisplayName).ToListAsync(cancellationToken);
        var roleNames = await db.Roles.AsNoTracking().ToDictionaryAsync(r => r.Id, r => r.Name, cancellationToken);

        IReadOnlyList<DemoUserDto> result = users
            .Select(u => new DemoUserDto(u.Email.Value, u.DisplayName, Describe(u, roleNames)))
            .ToList();

        return Result.Success(result);
    }

    private static string Describe(User user, Dictionary<Guid, string> roleNames)
    {
        var roles = user.RoleIds.Select(id => roleNames.GetValueOrDefault(id, "?")).Order().ToList();
        var description = roles.Count == 0 ? "No roles" : string.Join(", ", roles);
        return user.Status == UserStatus.Suspended ? $"{description} (suspended)" : description;
    }
}
