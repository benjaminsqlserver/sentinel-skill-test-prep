using Sentinel.Domain.Access;
using Sentinel.Domain.Common;

namespace Sentinel.Domain.Roles;

/// <summary>A named bundle of permissions, such as "Finance Analyst".</summary>
public sealed class Role : AggregateRoot
{
    public const int MaxNameLength = 64;

    // Not readonly: EF Core assigns a fresh list when it reads the column.
    private List<Permission> _permissions = [];

    private Role(Guid id, string name, string description) : base(id)
    {
        Name = name;
        Description = description;
    }

    // For EF Core.
    private Role() { }

    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;

    public IReadOnlyCollection<Permission> Permissions => _permissions.AsReadOnly();

    public static Result<Role> Create(string name, string? description = null, Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return RoleErrors.NameRequired;

        if (name.Trim().Length > MaxNameLength)
            return RoleErrors.NameTooLong;

        return new Role(id ?? Guid.CreateVersion7(), name.Trim(), description?.Trim() ?? string.Empty);
    }

    public Result Grant(Permission permission)
    {
        ArgumentNullException.ThrowIfNull(permission);

        if (_permissions.Contains(permission))
            return RoleErrors.AlreadyGranted(Name, permission);

        _permissions.Add(permission);
        return Result.Success();
    }

    public Result Revoke(Permission permission)
    {
        ArgumentNullException.ThrowIfNull(permission);

        return _permissions.Remove(permission)
            ? Result.Success()
            : RoleErrors.NotGranted(Name, permission);
    }
}

public static class RoleErrors
{
    public static readonly Error NameRequired =
        Error.Validation("Role.NameRequired", "A role name is required.");

    public static readonly Error NameTooLong =
        Error.Validation("Role.NameTooLong", $"A role name cannot exceed {Role.MaxNameLength} characters.");

    public static Error DuplicateName(string name) =>
        Error.Conflict("Role.DuplicateName", $"A role named '{name}' already exists.");

    public static Error NotFound(Guid id) =>
        Error.NotFound("Role.NotFound", $"Role {id} does not exist.");

    public static Error AlreadyGranted(string role, Permission permission) =>
        Error.Conflict("Role.AlreadyGranted", $"Role '{role}' already grants '{permission}'.");

    public static Error NotGranted(string role, Permission permission) =>
        Error.NotFound("Role.NotGranted", $"Role '{role}' does not grant '{permission}'.");
}
