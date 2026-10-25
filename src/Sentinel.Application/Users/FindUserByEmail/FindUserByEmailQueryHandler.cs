using Microsoft.EntityFrameworkCore;
using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Application.Abstractions.Ports;
using Sentinel.Domain.Common;
using Sentinel.Domain.Users;

namespace Sentinel.Application.Users.FindUserByEmail;

internal sealed class FindUserByEmailQueryHandler(IApplicationDbContext db) : IQueryHandler<FindUserByEmailQuery, Guid>
{
    public async Task<Result<Guid>> Handle(FindUserByEmailQuery request, CancellationToken cancellationToken)
    {
        var email = EmailAddress.Create(request.Email);
        if (email.IsFailure)
            return email.Error!;

        var id = await db.Users
            .AsNoTracking()
            .Where(u => u.Email == email.Value)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return id is null
            ? Error.NotFound("User.EmailNotFound", $"No user has the email '{email.Value}'.")
            : id.Value;
    }
}
