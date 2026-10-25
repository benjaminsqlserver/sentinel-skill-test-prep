using Sentinel.Api.Authentication;
using Sentinel.Api.Http;
using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Application.Pages.CreatePage;
using Sentinel.Application.Pages.GetPages;
using Sentinel.Contracts;

namespace Sentinel.Api.Endpoints;

public static class PageEndpoints
{
    public const string ReadPermission = "admin.access";
    public const string ManagePermission = "admin.pages.manage";

    public static IEndpointRouteBuilder MapPageEndpoints(this IEndpointRouteBuilder app)
    {
        var pages = app.MapGroup("/api/pages").WithTags("Pages");

        pages.MapGet("/", async (IDispatcher dispatcher, CancellationToken ct) =>
                (await dispatcher.Send(new GetPagesQuery(), ct)).ToOk())
            .RequirePermission(ReadPermission);

        pages.MapPost("/", async (CreatePageRequest body, IDispatcher dispatcher, CancellationToken ct) =>
                (await dispatcher.Send(new CreatePageCommand(
                        body.Path, body.Title, body.ParentPath, body.RequiredPermission, body.IsPublic, body.SortOrder), ct))
                    .ToCreated(id => $"/api/pages/{id}"))
            .RequirePermission(ManagePermission);

        return app;
    }
}
