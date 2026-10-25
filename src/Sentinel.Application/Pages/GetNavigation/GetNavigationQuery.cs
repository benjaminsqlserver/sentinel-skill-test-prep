using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Contracts;

namespace Sentinel.Application.Pages.GetNavigation;

/// <summary>The menu for the current user. A null user gets the public pages only.</summary>
public sealed record GetNavigationQuery(Guid? UserId) : IQuery<IReadOnlyList<NavigationNodeDto>>;
