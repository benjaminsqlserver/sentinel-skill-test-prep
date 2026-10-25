using Sentinel.Domain.Pages;

namespace Sentinel.Domain.Access;

public sealed record PageAccessDecision(Page Page, bool IsAllowed, AccessReason Reason);
