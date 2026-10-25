using Microsoft.EntityFrameworkCore;
using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Application.Abstractions.Ports;
using Sentinel.Application.Access;
using Sentinel.Contracts;
using Sentinel.Domain.Common;
using Sentinel.Domain.Users;

namespace Sentinel.Application.Users.GetCurrentUser;

internal sealed class GetCurrentUserQueryHandler(IApplicationDbContext db) : IQueryHandler<GetCurrentUserQuery, CurrentUserDto>
{
    public async Task<Result<CurrentUserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
            return UserErrors.NotFound(request.UserId);

        var subject = await db.LoadAccessSubjectAsync(user.Id, cancellationToken);

        // Informational only: the UI uses this to hide buttons, never to
        // decide access. The server re-checks on every request.
        var permissions = subject!.Granted
            .Where(p => !subject.IsDenied(p))
            .Select(p => p.Value)
            .Order(StringComparer.Ordinal)
            .ToList();

        return new CurrentUserDto(user.Id, user.Email.Value, user.DisplayName, user.TimeZoneId, permissions);
    }
}
