namespace Sentinel.Contracts;

public sealed record RoleSummaryDto(Guid Id, string Name);

public sealed record RoleDto(Guid Id, string Name, string Description, IReadOnlyList<string> Permissions, int UserCount);

public sealed record CreateRoleRequest(string Name, string? Description);

public sealed record GrantPermissionRequest(string Permission);
