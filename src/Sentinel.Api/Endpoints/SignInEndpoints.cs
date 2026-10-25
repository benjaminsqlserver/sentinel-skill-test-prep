using Sentinel.Api.Authentication;
using Sentinel.Api.Http;
using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Application.Security.RecordSignIn;
using Sentinel.Application.Users.GetDemoUsers;
using Sentinel.Contracts;

namespace Sentinel.Api.Endpoints;

public static class SignInEndpoints
{
    public const string IngestPermission = "security.ingest";

    public static IEndpointRouteBuilder MapSignInEndpoints(this IEndpointRouteBuilder app)
    {
        // Called by the identity provider after each attempt. In this book the
        // "identity provider" is you, with curl or the .http file.
        app.MapPost("/api/sign-ins", async (RecordSignInRequest body, IDispatcher dispatcher, CancellationToken ct) =>
                (await dispatcher.Send(new RecordSignInCommand(
                        body.Email, body.OccurredAt, body.IpAddress, body.UserAgent, body.Succeeded, body.FailureReason), ct))
                    .ToCreated(id => $"/api/sign-ins/{id}"))
            .WithTags("Sign-ins")
            .RequirePermission(IngestPermission);

        return app;
    }

    /// <summary>The user switcher's data source. Mapped only in Development.</summary>
    public static IEndpointRouteBuilder MapDevelopmentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/dev/demo-users", async (IDispatcher dispatcher, CancellationToken ct) =>
                (await dispatcher.Send(new GetDemoUsersQuery(), ct)).ToOk())
            .WithTags("Development");

        return app;
    }
}
