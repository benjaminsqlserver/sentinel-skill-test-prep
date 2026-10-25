using System.Security.Claims;
using Sentinel.Api.Authentication;
using Sentinel.Api.Http;
using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Application.Pages.CheckPageAccess;
using Sentinel.Application.Pages.GetNavigation;
using Sentinel.Application.Security.AcknowledgeFinding;
using Sentinel.Application.Security.GetAccountSecurityStatus;
using Sentinel.Application.Security.GetSignInHistory;
using Sentinel.Application.Users.GetCurrentUser;

namespace Sentinel.Api.Endpoints;

/// <summary>Endpoints about the caller themselves. No admin permission involved.</summary>
public static class MeEndpoints
{
    public static IEndpointRouteBuilder MapMeEndpoints(this IEndpointRouteBuilder app)
    {
        var me = app.MapGroup("/api/me").WithTags("Me");

        me.MapGet("/", async (ClaimsPrincipal user, IDispatcher dispatcher, CancellationToken ct) =>
                (await dispatcher.Send(new GetCurrentUserQuery(user.GetRequiredUserId()), ct)).ToOk())
            .RequireAuthorization();

        // The two access endpoints allow anonymous callers: a signed-out
        // visitor still needs a menu (the public pages) and a guard verdict.
        me.MapGet("/navigation", async (ClaimsPrincipal user, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new GetNavigationQuery(user.GetUserId()), ct)).ToOk());

        me.MapGet("/page-access", async (string path, ClaimsPrincipal user, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new CheckPageAccessQuery(user.GetUserId(), path), ct)).ToOk());

        me.MapGet("/security", async (ClaimsPrincipal user, IDispatcher dispatcher, CancellationToken ct) =>
                (await dispatcher.Send(new GetAccountSecurityStatusQuery(user.GetRequiredUserId()), ct)).ToOk())
            .RequireAuthorization();

        me.MapGet("/sign-ins", async (ClaimsPrincipal user, IDispatcher dispatcher, CancellationToken ct, int page = 1, int pageSize = 50) =>
                (await dispatcher.Send(new GetSignInHistoryQuery(user.GetRequiredUserId(), page, pageSize), ct)).ToOk())
            .RequireAuthorization();

        me.MapPost("/security/findings/{key}/acknowledge", async (string key, ClaimsPrincipal user, IDispatcher dispatcher, CancellationToken ct) =>
                (await dispatcher.Send(new AcknowledgeFindingCommand(user.GetRequiredUserId(), key), ct)).ToNoContent())
            .RequireAuthorization();

        return app;
    }
}
