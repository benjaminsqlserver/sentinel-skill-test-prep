using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Application.Access.HasPermission;

namespace Sentinel.Api.Authentication;

/// <summary>
/// Guards an endpoint with a permission, evaluated by the same domain rules
/// that build the menu. "Hidden in the UI" is not security; this is.
/// </summary>
public static class PermissionFilter
{
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permission)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.RequireAuthorization();
        builder.AddEndpointFilter(async (context, next) =>
        {
            var http = context.HttpContext;
            var userId = http.User.GetRequiredUserId();
            var dispatcher = http.RequestServices.GetRequiredService<IDispatcher>();

            var allowed = await dispatcher.Send(new HasPermissionQuery(userId, permission), http.RequestAborted);

            return allowed is { IsSuccess: true, Value: true }
                ? await next(context)
                : TypedResults.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Forbidden",
                    detail: $"This action requires the '{permission}' permission.");
        });

        return builder;
    }
}
