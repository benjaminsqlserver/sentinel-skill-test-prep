using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Contracts;

namespace Sentinel.Application.Roles.GetRoles;

public sealed record GetRolesQuery : IQuery<IReadOnlyList<RoleDto>>;
