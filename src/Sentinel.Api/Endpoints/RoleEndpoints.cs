using Sentinel.Api.Authentication;
using Sentinel.Api.Http;
using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Application.Roles.CreateRole;
using Sentinel.Application.Roles.GetRoles;
using Sentinel.Application.Roles.RolePermissions;
using Sentinel.Contracts;

namespace Sentinel.Api.Endpoints;

public static class RoleEndpoints
{
    public const string ReadPermission = "admin.roles";
    public const string ManagePermission = "admin.roles.manage";

    public static IEndpointRouteBuilder MapRoleEndpoints(this IEndpointRouteBuilder app)
    {
        var roles = app.MapGroup("/api/roles").WithTags("Roles");

        roles.MapGet("/", async (IDispatcher dispatcher, CancellationToken ct) =>
                (await dispatcher.Send(new GetRolesQuery(), ct)).ToOk())
            .RequirePermission(ReadPermission);

        roles.MapPost("/", async (CreateRoleRequest body, IDispatcher dispatcher, CancellationToken ct) =>
                (await dispatcher.Send(new CreateRoleCommand(body.Name, body.Description), ct))
                    .ToCreated(id => $"/api/roles/{id}"))
            .RequirePermission(ManagePermission);

        roles.MapPost("/{id:guid}/permissions", async (Guid id, GrantPermissionRequest body, IDispatcher dispatcher, CancellationToken ct) =>
                (await dispatcher.Send(new GrantPermissionCommand(id, body.Permission), ct)).ToNoContent())
            .RequirePermission(ManagePermission);

        roles.MapDelete("/{id:guid}/permissions/{permission}", async (Guid id, string permission, IDispatcher dispatcher, CancellationToken ct) =>
                (await dispatcher.Send(new RevokePermissionCommand(id, permission), ct)).ToNoContent())
            .RequirePermission(ManagePermission);

        return app;
    }
}
