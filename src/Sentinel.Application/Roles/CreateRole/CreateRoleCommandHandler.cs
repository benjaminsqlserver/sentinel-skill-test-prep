using Microsoft.EntityFrameworkCore;
using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Application.Abstractions.Ports;
using Sentinel.Domain.Common;
using Sentinel.Domain.Roles;

namespace Sentinel.Application.Roles.CreateRole;

internal sealed class CreateRoleCommandHandler(IApplicationDbContext db) : ICommandHandler<CreateRoleCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = Role.Create(request.Name, request.Description);
        if (role.IsFailure)
            return role.Error!;

        // ToUpper on both sides: SQL Server compares case-insensitively by
        // default and SQLite does not. Say what we mean instead of relying
        // on the collation of whichever database the tests happen to use.
        var name = role.Value.Name.ToUpperInvariant();
        if (await db.Roles.AnyAsync(r => r.Name.ToUpper() == name, cancellationToken))
            return RoleErrors.DuplicateName(role.Value.Name);

        db.Roles.Add(role.Value);
        await db.SaveChangesAsync(cancellationToken);
        return role.Value.Id;
    }
}
