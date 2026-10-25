using Microsoft.EntityFrameworkCore;
using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Application.Abstractions.Ports;
using Sentinel.Contracts;
using Sentinel.Domain.Common;
using Sentinel.Domain.Users;

namespace Sentinel.Application.Users.GetUsers;

internal sealed class GetUsersQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetUsersQuery, PagedResponse<UserSummaryDto>>
{
    public async Task<Result<PagedResponse<UserSummaryDto>>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var users = db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();

            // Email is stored through a value converter, so it can be compared
            // whole but not searched by substring. Matching an exact address is
            // what an administrator pasting from a ticket actually needs.
            var email = EmailAddress.Create(term);
            users = email.IsSuccess
                ? users.Where(u => u.Email == email.Value)
                : users.Where(u => u.DisplayName.Contains(term));
        }

        var total = await users.CountAsync(cancellationToken);

        var page = await users
            .OrderBy(u => u.DisplayName)
            .ThenBy(u => u.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var roleNames = await db.Roles
            .AsNoTracking()
            .ToDictionaryAsync(r => r.Id, r => r.Name, cancellationToken);

        var items = page
            .Select(u => new UserSummaryDto(
                u.Id,
                u.Email.Value,
                u.DisplayName,
                u.Status.ToString(),
                u.RoleIds.Select(id => roleNames.GetValueOrDefault(id, "(deleted role)")).Order().ToList()))
            .ToList();

        return new PagedResponse<UserSummaryDto>(items, request.Page, request.PageSize, total);
    }
}
