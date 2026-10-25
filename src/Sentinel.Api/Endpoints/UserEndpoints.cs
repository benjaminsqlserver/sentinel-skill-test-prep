using Sentinel.Api.Authentication;
using Sentinel.Api.Http;
using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Application.Pages.GetPageAccess;
using Sentinel.Application.Security.GetAccountSecurityStatus;
using Sentinel.Application.Users.CreateUser;
using Sentinel.Application.Users.GetUserById;
using Sentinel.Application.Users.GetUsers;
using Sentinel.Application.Users.PermissionOverrides;
using Sentinel.Application.Users.RoleAssignment;
using Sentinel.Application.Users.Suspension;
using Sentinel.Contracts;

namespace Sentinel.Api.Endpoints;

/// <summary>
/// User administration. Reading needs <c>admin.users</c>; changing needs
/// <c>admin.users.manage</c>, so an auditor can look without touching.
/// </summary>
public static class UserEndpoints
{
    public const string ReadPermission = "admin.users";
    public const string ManagePermission = "admin.users.manage";

    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var users = app.MapGroup("/api/users").WithTags("Users");

        users.MapGet("/", async (IDispatcher dispatcher, CancellationToken ct, string? search, int page = 1, int pageSize = 25) =>
                (await dispatcher.Send(new GetUsersQuery(search, page, pageSize), ct)).ToOk())
            .RequirePermission(ReadPermission);

        users.MapGet("/{id:guid}", async (Guid id, IDispatcher dispatcher, CancellationToken ct) =>
                (await dispatcher.Send(new GetUserByIdQuery(id), ct)).ToOk())
            .RequirePermission(ReadPermission);

        users.MapGet("/{id:guid}/page-access", async (Guid id, IDispatcher dispatcher, CancellationToken ct) =>
                (await dispatcher.Send(new GetPageAccessQuery(id), ct)).ToOk())
            .RequirePermission(ReadPermission);

        users.MapGet("/{id:guid}/security", async (Guid id, IDispatcher dispatcher, CancellationToken ct) =>
                (await dispatcher.Send(new GetAccountSecurityStatusQuery(id), ct)).ToOk())
            .RequirePermission(ReadPermission);

        users.MapPost("/", async (CreateUserRequest body, IDispatcher dispatcher, CancellationToken ct) =>
                (await dispatcher.Send(new CreateUserCommand(body.Email, body.DisplayName, body.TimeZoneId), ct))
                    .ToCreated(id => $"/api/users/{id}"))
            .RequirePermission(ManagePermission);

        users.MapPost("/{id:guid}/roles", async (Guid id, AssignRoleRequest body, IDispatcher dispatcher, CancellationToken ct) =>
                (await dispatcher.Send(new AssignRoleCommand(id, body.RoleId), ct)).ToNoContent())
            .RequirePermission(ManagePermission);

        users.MapDelete("/{id:guid}/roles/{roleId:guid}", async (Guid id, Guid roleId, IDispatcher dispatcher, CancellationToken ct) =>
                (await dispatcher.Send(new RevokeRoleCommand(id, roleId), ct)).ToNoContent())
            .RequirePermission(ManagePermission);

        users.MapPost("/{id:guid}/suspend", async (Guid id, SuspendUserRequest body, IDispatcher dispatcher, CancellationToken ct) =>
                (await dispatcher.Send(new SuspendUserCommand(id, body.Reason), ct)).ToNoContent())
            .RequirePermission(ManagePermission);

        users.MapPost("/{id:guid}/reactivate", async (Guid id, IDispatcher dispatcher, CancellationToken ct) =>
                (await dispatcher.Send(new ReactivateUserCommand(id), ct)).ToNoContent())
            .RequirePermission(ManagePermission);

        users.MapPut("/{id:guid}/overrides", async (Guid id, SetPermissionOverrideRequest body, IDispatcher dispatcher, CancellationToken ct) =>
                (await dispatcher.Send(new SetPermissionOverrideCommand(id, body.Permission, body.Effect), ct)).ToNoContent())
            .RequirePermission(ManagePermission);

        users.MapDelete("/{id:guid}/overrides/{permission}", async (Guid id, string permission, IDispatcher dispatcher, CancellationToken ct) =>
                (await dispatcher.Send(new ClearPermissionOverrideCommand(id, permission), ct)).ToNoContent())
            .RequirePermission(ManagePermission);

        return app;
    }
}
