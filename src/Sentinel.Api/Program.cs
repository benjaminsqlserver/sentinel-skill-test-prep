using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Sentinel.Api.Authentication;
using Sentinel.Api.Endpoints;
using Sentinel.Application;
using Sentinel.Infrastructure;
using Sentinel.Infrastructure.Persistence;
using Sentinel.Infrastructure.Persistence.Seeding;

var builder = WebApplication.CreateBuilder(args);

// ---- Composition root: the only place that knows every layer. ----
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services
    .AddAuthentication(DemoUserAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, DemoUserAuthenticationHandler>(DemoUserAuthenticationHandler.SchemeName, null);
builder.Services.AddAuthorization();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    // The demo scheme trusts a header. That is acceptable on a laptop and
    // nowhere else, so fail loudly rather than run insecurely by accident.
    throw new InvalidOperationException(
        "Sentinel is configured with development-only authentication. Replace DemoUserAuthenticationHandler before deploying.");
}

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapMeEndpoints();
app.MapUserEndpoints();
app.MapRoleEndpoints();
app.MapPageEndpoints();
app.MapSignInEndpoints();
app.MapDevelopmentEndpoints();
app.MapGet("/health", () => TypedResults.Ok(new { status = "ok" })).WithTags("Health");

if (app.Configuration.GetValue("Database:MigrateAndSeedOnStartup", true))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<SentinelDbContext>();
    await db.Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>().SeedAsync();
}

await app.RunAsync();

/// <summary>Exposed so WebApplicationFactory&lt;Program&gt; can host the API in tests.</summary>
public partial class Program;
