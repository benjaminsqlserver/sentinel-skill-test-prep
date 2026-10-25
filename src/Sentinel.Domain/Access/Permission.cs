using System.Text.RegularExpressions;
using Sentinel.Domain.Common;

namespace Sentinel.Domain.Access;

/// <summary>
/// A named capability such as <c>reports.finance</c>. Permissions are
/// dot-separated, lower-case segments. A grant may end in a wildcard:
/// <c>reports.*</c> covers every permission beneath <c>reports</c>, and
/// <c>*</c> covers everything.
/// </summary>
/// <remarks>
/// Wildcards are only meaningful on the granting side. A page *requires*
/// a concrete permission; a role may *grant* a pattern. <c>reports.*</c>
/// deliberately does not cover <c>reports</c> itself: "everything under
/// reports" is not the same claim as "the reports landing page".
/// </remarks>
public sealed partial class Permission : IEquatable<Permission>
{
    public const string Wildcard = "*";

    private Permission(string value) => Value = value;

    public string Value { get; }

    public bool IsWildcard => Value == Wildcard || Value.EndsWith(".*", StringComparison.Ordinal);

    public static Result<Permission> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return PermissionErrors.Blank;

        var normalised = value.Trim().ToLowerInvariant();

        return normalised == Wildcard || PermissionPattern().IsMatch(normalised)
            ? new Permission(normalised)
            : PermissionErrors.Malformed(value);
    }

    /// <summary>
    /// For values already validated: seed data and database rows. Throws on
    /// bad input because bad input here is a bug, not a user mistake.
    /// </summary>
    public static Permission From(string value)
    {
        var result = Create(value);
        return result.IsSuccess
            ? result.Value
            : throw new ArgumentException(result.Error!.Message, nameof(value));
    }

    /// <summary>Does this grant (or deny) cover the <paramref name="required"/> permission?</summary>
    public bool Covers(Permission required)
    {
        ArgumentNullException.ThrowIfNull(required);

        if (Value == Wildcard)
            return true;

        if (!IsWildcard)
            return Value == required.Value;

        // "reports.*" -> the prefix "reports." must start the required value.
        var prefix = Value[..^1];
        return required.Value.StartsWith(prefix, StringComparison.Ordinal);
    }

    public bool Equals(Permission? other) => other is not null && other.Value == Value;
    public override bool Equals(object? obj) => Equals(obj as Permission);
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);
    public override string ToString() => Value;

    // segment(.segment)* optionally followed by ".*"
    [GeneratedRegex(@"^[a-z0-9][a-z0-9_-]*(\.[a-z0-9][a-z0-9_-]*)*(\.\*)?$")]
    private static partial Regex PermissionPattern();
}

public static class PermissionErrors
{
    public static readonly Error Blank =
        Error.Validation("Permission.Blank", "A permission name is required.");

    public static Error Malformed(string value) =>
        Error.Validation("Permission.Malformed",
            $"'{value}' is not a valid permission. Use dot-separated segments such as 'reports.finance' or 'reports.*'.");
}
