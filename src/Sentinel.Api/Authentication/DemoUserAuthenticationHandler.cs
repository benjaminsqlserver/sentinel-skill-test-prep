using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Application.Users.FindUserByEmail;

namespace Sentinel.Api.Authentication;

/// <summary>
/// Development-only authentication: the caller says who they are in an
/// <c>X-Demo-User</c> header and we believe them. It exists so the book can
/// switch between eight users in one click without an identity provider.
/// </summary>
/// <remarks>
/// In production, replace this scheme with JWT bearer or OpenID Connect.
/// Nothing else changes: endpoints read the user id from the
/// <see cref="SentinelClaims.UserId"/> claim, whoever put it there.
/// Program.cs refuses to start with this scheme outside Development.
/// </remarks>
public sealed class DemoUserAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IDispatcher dispatcher)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Demo";
    public const string HeaderName = "X-Demo-User";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var values) || string.IsNullOrWhiteSpace(values.ToString()))
            return AuthenticateResult.NoResult(); // anonymous, not a failure

        var email = values.ToString().Trim();
        var userId = await dispatcher.Send(new FindUserByEmailQuery(email), Context.RequestAborted);
        if (userId.IsFailure)
            return AuthenticateResult.Fail($"Unknown demo user '{email}'.");

        var identity = new ClaimsIdentity(
            [
                new Claim(SentinelClaims.UserId, userId.Value.ToString()),
                new Claim(ClaimTypes.Email, email)
            ],
            SchemeName);

        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
    }
}
