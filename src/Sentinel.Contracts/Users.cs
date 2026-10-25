namespace Sentinel.Contracts;

public sealed record UserSummaryDto(
    Guid Id,
    string Email,
    string DisplayName,
    string Status,
    IReadOnlyList<string> Roles);

public sealed record UserDetailDto(
    Guid Id,
    string Email,
    string DisplayName,
    string TimeZoneId,
    string Status,
    string? SuspensionReason,
    DateTime CreatedAtUtc,
    IReadOnlyList<RoleSummaryDto> Roles,
    IReadOnlyList<PermissionOverrideDto> Overrides);

public sealed record PermissionOverrideDto(string Permission, string Effect);

/// <summary>The signed-in user, as the shell needs them.</summary>
public sealed record CurrentUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    string TimeZoneId,
    IReadOnlyList<string> Permissions);

/// <summary>The public list behind the demo user switcher. Development only.</summary>
public sealed record DemoUserDto(string Email, string DisplayName, string Description);

public sealed record CreateUserRequest(string Email, string DisplayName, string TimeZoneId);

public sealed record AssignRoleRequest(Guid RoleId);

public sealed record SuspendUserRequest(string Reason);

public sealed record SetPermissionOverrideRequest(string Permission, string Effect);
