using System.Security.Claims;

namespace Sentinel.Api.Authentication;

public static class SentinelClaims
{
    public const string UserId = "sentinel:user_id";

    /// <summary>The signed-in user's id, or null for an anonymous request.</summary>
    public static Guid? GetUserId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(UserId), out var id) ? id : null;

    /// <summary>For endpoints that already require authentication.</summary>
    public static Guid GetRequiredUserId(this ClaimsPrincipal principal) =>
        principal.GetUserId() ?? throw new InvalidOperationException("The endpoint requires authentication but no user id claim is present.");
}
