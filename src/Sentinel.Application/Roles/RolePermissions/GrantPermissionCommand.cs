using Sentinel.Application.Abstractions.Messaging;

namespace Sentinel.Application.Roles.RolePermissions;

public sealed record GrantPermissionCommand(Guid RoleId, string Permission) : ICommand;

public sealed record RevokePermissionCommand(Guid RoleId, string Permission) : ICommand;
