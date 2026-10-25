using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Sentinel.Api.Authentication;
using Sentinel.Infrastructure.Persistence;
using Sentinel.Infrastructure.Persistence.Seeding;

namespace Sentinel.Api.Tests;

/// <summary>
/// The whole API, in process, over an in-memory SQLite database seeded with
/// the development data. One factory per test class (xUnit's class fixture),
/// so tests in a class share a database: write tests that do not depend on
/// each other's changes.
/// </summary>
public sealed class SentinelApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.UseEnvironment("Development");

        // The migrations are SQL Server's. Tests build the schema from the model instead.
        builder.UseSetting("Database:MigrateAndSeedOnStartup", "false");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<SentinelDbContext>>();
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<SentinelDbContext>));
            services.AddDbContext<SentinelDbContext>(o => o.UseSqlite(_connection));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<SentinelDbContext>().Database.EnsureCreated();
        scope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>().SeedAsync().GetAwaiter().GetResult();

        return host;
    }

    /// <summary>A client signed in as <paramref name="email"/>, or anonymous when null.</summary>
    public HttpClient ClientFor(string? email)
    {
        var client = CreateClient();
        if (email is not null)
            client.DefaultRequestHeaders.Add(DemoUserAuthenticationHandler.HeaderName, email);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}
