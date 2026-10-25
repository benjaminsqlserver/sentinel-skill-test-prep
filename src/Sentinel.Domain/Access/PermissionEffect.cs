namespace Sentinel.Domain.Access;

/// <summary>Whether a user-level override adds a permission or takes it away.</summary>
public enum PermissionEffect
{
    Allow,
    Deny
}
