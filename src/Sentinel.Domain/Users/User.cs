using Sentinel.Domain.Access;
using Sentinel.Domain.Common;

namespace Sentinel.Domain.Users;

public enum UserStatus
{
    Active,
    Suspended
}

/// <summary>A role held by a user. Part of the <see cref="User"/> aggregate.</summary>
public sealed class UserRole
{
    internal UserRole(Guid roleId, DateTime assignedAtUtc)
    {
        RoleId = roleId;
        AssignedAtUtc = assignedAtUtc;
    }

    // For EF Core.
    private UserRole() { }

    public Guid RoleId { get; private set; }
    public DateTime AssignedAtUtc { get; private set; }
}

/// <summary>
/// A permission granted or denied to one user directly, regardless of role.
/// Denies exist for the "everyone in Finance except the contractor" case.
/// </summary>
public sealed class PermissionOverride
{
    internal PermissionOverride(Permission permission, PermissionEffect effect)
    {
        Permission = permission;
        Effect = effect;
    }

    // For EF Core.
    private PermissionOverride() { }

    public Permission Permission { get; private set; } = null!;
    public PermissionEffect Effect { get; private set; }
}

/// <summary>A person who signs in to the portal.</summary>
public sealed class User : AggregateRoot
{
    public const int MaxDisplayNameLength = 100;

    private readonly List<UserRole> _roles = [];
    private readonly List<PermissionOverride> _overrides = [];

    private User(Guid id, EmailAddress email, string displayName, string timeZoneId, DateTime createdAtUtc) : base(id)
    {
        Email = email;
        DisplayName = displayName;
        TimeZoneId = timeZoneId;
        CreatedAtUtc = createdAtUtc;
        Status = UserStatus.Active;
    }

    // For EF Core.
    private User() { }

    public EmailAddress Email { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;

    /// <summary>
    /// An IANA zone such as <c>Africa/Lagos</c>. Timestamps are stored in UTC
    /// and converted to this zone only at the moment they are shown.
    /// </summary>
    public string TimeZoneId { get; private set; } = null!;

    public UserStatus Status { get; private set; }
    public string? SuspensionReason { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<UserRole> Roles => _roles.AsReadOnly();
    public IEnumerable<Guid> RoleIds => _roles.Select(r => r.RoleId);
    public IReadOnlyCollection<PermissionOverride> Overrides => _overrides.AsReadOnly();

    public static Result<User> Create(EmailAddress email, string displayName, string timeZoneId, DateTime nowUtc, Guid? id = null)
    {
        ArgumentNullException.ThrowIfNull(email);

        if (string.IsNullOrWhiteSpace(displayName))
            return UserErrors.DisplayNameRequired;

        if (displayName.Trim().Length > MaxDisplayNameLength)
            return UserErrors.DisplayNameTooLong;

        if (!IsKnownTimeZone(timeZoneId))
            return UserErrors.UnknownTimeZone(timeZoneId);

        return new User(id ?? Guid.CreateVersion7(), email, displayName.Trim(), timeZoneId, EnsureUtc(nowUtc));
    }

    public Result AssignRole(Guid roleId, DateTime nowUtc)
    {
        if (_roles.Any(r => r.RoleId == roleId))
            return UserErrors.RoleAlreadyAssigned(roleId);

        _roles.Add(new UserRole(roleId, EnsureUtc(nowUtc)));
        return Result.Success();
    }

    public Result RevokeRole(Guid roleId)
    {
        var held = _roles.FirstOrDefault(r => r.RoleId == roleId);
        if (held is null)
            return UserErrors.RoleNotAssigned(roleId);

        _roles.Remove(held);
        return Result.Success();
    }

    /// <summary>
    /// Adds or replaces the user's override for <paramref name="permission"/>.
    /// A user has at most one override per permission: allowing and denying
    /// the same thing at once has no meaning.
    /// </summary>
    public Result SetOverride(Permission permission, PermissionEffect effect)
    {
        ArgumentNullException.ThrowIfNull(permission);

        _overrides.RemoveAll(o => o.Permission == permission);
        _overrides.Add(new PermissionOverride(permission, effect));
        return Result.Success();
    }

    public Result ClearOverride(Permission permission)
    {
        ArgumentNullException.ThrowIfNull(permission);

        return _overrides.RemoveAll(o => o.Permission == permission) > 0
            ? Result.Success()
            : UserErrors.OverrideNotFound(permission);
    }

    public Result Suspend(string reason)
    {
        if (Status == UserStatus.Suspended)
            return UserErrors.AlreadySuspended;

        if (string.IsNullOrWhiteSpace(reason))
            return UserErrors.SuspensionReasonRequired;

        Status = UserStatus.Suspended;
        SuspensionReason = reason.Trim();
        return Result.Success();
    }

    public Result Reactivate()
    {
        if (Status == UserStatus.Active)
            return UserErrors.NotSuspended;

        Status = UserStatus.Active;
        SuspensionReason = null;
        return Result.Success();
    }

    private static bool IsKnownTimeZone(string? timeZoneId) =>
        !string.IsNullOrWhiteSpace(timeZoneId) && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out _);

    private static DateTime EnsureUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc
            ? value
            : throw new ArgumentException("Timestamps must be UTC.", nameof(value));
}

public static class UserErrors
{
    public static readonly Error EmailRequired =
        Error.Validation("User.EmailRequired", "An email address is required.");

    public static Error EmailInvalid(string? email) =>
        Error.Validation("User.EmailInvalid", $"'{email}' is not a valid email address.");

    public static readonly Error DisplayNameRequired =
        Error.Validation("User.DisplayNameRequired", "A display name is required.");

    public static readonly Error DisplayNameTooLong =
        Error.Validation("User.DisplayNameTooLong", $"A display name cannot exceed {User.MaxDisplayNameLength} characters.");

    public static Error UnknownTimeZone(string? zone) =>
        Error.Validation("User.UnknownTimeZone", $"'{zone}' is not a recognised time zone. Use an IANA name such as 'Africa/Lagos'.");

    public static Error DuplicateEmail(EmailAddress email) =>
        Error.Conflict("User.DuplicateEmail", $"A user with email '{email}' already exists.");

    public static Error NotFound(Guid id) =>
        Error.NotFound("User.NotFound", $"User {id} does not exist.");

    public static Error RoleAlreadyAssigned(Guid roleId) =>
        Error.Conflict("User.RoleAlreadyAssigned", $"The user already holds role {roleId}.");

    public static Error RoleNotAssigned(Guid roleId) =>
        Error.NotFound("User.RoleNotAssigned", $"The user does not hold role {roleId}.");

    public static Error OverrideNotFound(Permission permission) =>
        Error.NotFound("User.OverrideNotFound", $"The user has no override for '{permission}'.");

    public static readonly Error AlreadySuspended =
        Error.Conflict("User.AlreadySuspended", "The user is already suspended.");

    public static readonly Error NotSuspended =
        Error.Conflict("User.NotSuspended", "The user is not suspended.");

    public static readonly Error SuspensionReasonRequired =
        Error.Validation("User.SuspensionReasonRequired", "A reason is required to suspend a user.");
}
