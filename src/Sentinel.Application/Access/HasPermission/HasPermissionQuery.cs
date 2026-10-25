using Sentinel.Application.Abstractions.Messaging;

namespace Sentinel.Application.Access.HasPermission;

/// <summary>
/// Does this user hold this permission right now? Used by the API to guard
/// admin endpoints, so that the screens and the endpoints behind them are
/// protected by exactly the same rules.
/// </summary>
public sealed record HasPermissionQuery(Guid UserId, string Permission) : IQuery<bool>;
