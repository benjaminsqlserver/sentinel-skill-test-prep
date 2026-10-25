namespace Sentinel.Domain.Access;

/// <summary>
/// Why a page was allowed or denied. Every decision carries one, because
/// "you can't see this" without a reason is tomorrow's support ticket.
/// </summary>
public enum AccessReason
{
    // Allowed
    PublicPage,
    AuthenticatedUser,
    PermissionGranted,

    // Denied
    NotAuthenticated,
    UserSuspended,
    MissingPermission,
    ExplicitlyDenied,
    ParentDenied,
    OrphanedPage,
    CyclicHierarchy
}
