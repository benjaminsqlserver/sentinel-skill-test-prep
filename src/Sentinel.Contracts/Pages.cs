namespace Sentinel.Contracts;

public sealed record PageDto(
    Guid Id,
    string Path,
    string Title,
    Guid? ParentId,
    string? RequiredPermission,
    bool IsPublic,
    int SortOrder);

/// <summary>
/// One page and the verdict for one user. <c>Reason</c> is the
/// <c>AccessReason</c> name ("MissingPermission", "ParentDenied"...), so the
/// admin screen can explain a denial instead of just reporting it.
/// </summary>
public sealed record PageAccessDto(
    string Path,
    string Title,
    string? ParentPath,
    string? RequiredPermission,
    bool IsAllowed,
    string Reason);

/// <summary>A node in the navigation menu. Only allowed pages appear.</summary>
public sealed record NavigationNodeDto(string Path, string Title, IReadOnlyList<NavigationNodeDto> Children);

public sealed record CreatePageRequest(
    string Path,
    string Title,
    string? ParentPath,
    string? RequiredPermission,
    bool IsPublic,
    int SortOrder);
