using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Contracts;

namespace Sentinel.Application.Pages.GetPageAccess;

/// <summary>
/// Every page, with the verdict and the reason, for one user. This is the
/// admin "access explorer": the answer to a ticket that says "Ada can't see
/// the finance report" is on this screen.
/// </summary>
public sealed record GetPageAccessQuery(Guid UserId) : IQuery<IReadOnlyList<PageAccessDto>>;
