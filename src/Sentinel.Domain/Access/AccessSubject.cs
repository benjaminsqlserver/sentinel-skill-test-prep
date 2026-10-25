using Sentinel.Domain.Roles;
using Sentinel.Domain.Users;

namespace Sentinel.Domain.Access;

public enum SubjectStatus
{
    Anonymous,
    Active,
    Suspended
}

/// <summary>
/// Everything the access rules need to know about the person asking, and
/// nothing else. Flattening the user and their roles into this shape is what
/// lets <see cref="PageAccessEvaluator"/> stay a pure function: no database,
/// no clock, no HTTP context.
/// </summary>
public sealed class AccessSubject
{
    private AccessSubject(SubjectStatus status, IReadOnlyCollection<Permission> granted, IReadOnlyCollection<Permission> denied)
    {
        Status = status;
        Granted = granted;
        Denied = denied;
    }

    public SubjectStatus Status { get; }
    public IReadOnlyCollection<Permission> Granted { get; }
    public IReadOnlyCollection<Permission> Denied { get; }

    public static AccessSubject Anonymous { get; } = new(SubjectStatus.Anonymous, [], []);

    public static AccessSubject Create(SubjectStatus status, IEnumerable<Permission> granted, IEnumerable<Permission> denied) =>
        new(status, granted.Distinct().ToList(), denied.Distinct().ToList());

    /// <summary>
    /// Builds the subject for <paramref name="user"/>. <paramref name="roles"/> may
    /// contain roles the user does not hold (only the user's own roles count),
    /// so callers can pass every role without filtering first.
    /// </summary>
    public static AccessSubject For(User user, IEnumerable<Role> roles)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(roles);

        var heldRoleIds = user.RoleIds.ToHashSet();

        var granted = roles
            .Where(role => heldRoleIds.Contains(role.Id))
            .SelectMany(role => role.Permissions)
            .Concat(user.Overrides.Where(o => o.Effect == PermissionEffect.Allow).Select(o => o.Permission));

        var denied = user.Overrides
            .Where(o => o.Effect == PermissionEffect.Deny)
            .Select(o => o.Permission);

        var status = user.Status == UserStatus.Active ? SubjectStatus.Active : SubjectStatus.Suspended;
        return Create(status, granted, denied);
    }

    public bool IsDenied(Permission required) => Denied.Any(d => d.Covers(required));

    public bool IsGranted(Permission required) => Granted.Any(g => g.Covers(required));

    /// <summary>Deny always wins over allow, whichever role the allow came from.</summary>
    public bool Has(Permission required) =>
        Status == SubjectStatus.Active && !IsDenied(required) && IsGranted(required);
}
