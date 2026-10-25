using Sentinel.Application.Abstractions.Messaging;

namespace Sentinel.Application.Users.RoleAssignment;

public sealed record AssignRoleCommand(Guid UserId, Guid RoleId) : ICommand;

public sealed record RevokeRoleCommand(Guid UserId, Guid RoleId) : ICommand;
