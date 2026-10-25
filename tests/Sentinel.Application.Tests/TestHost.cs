using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sentinel.Application;
using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Application.Abstractions.Ports;
using Sentinel.Domain.Common;
using Sentinel.Infrastructure.Persistence;
using Sentinel.Infrastructure.Persistence.Seeding;
using Sentinel.Infrastructure.Services;

namespace Sentinel.Application.Tests;

public sealed class FakeClock(DateTime utcNow) : IClock
{
    public DateTime UtcNow { get; set; } = utcNow;
}

/// <summary>
/// The real Application layer and the real EF Core model, over an in-memory
/// SQLite database that lives as long as this object. Handlers run through
/// the real dispatcher and pipeline, so a test here exercises exactly what an
/// endpoint would, minus HTTP.
/// </summary>
/// <remarks>
/// SQLite is not SQL Server. It is close enough for the queries in this book,
/// and the differences that matter (case sensitivity, mainly) are handled
/// explicitly in the handlers. Run the API tests against SQL Server in CI if
/// your queries grow provider-specific.
/// </remarks>
public sealed class TestHost : IAsyncDisposable
{
    public static readonly DateTime Now = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _services;

    private TestHost(SqliteConnection connection, ServiceProvider services)
    {
        _connection = connection;
        _services = services;
    }

    public FakeClock Clock => (FakeClock)_services.GetRequiredService<IClock>();

    public static async Task<TestHost> CreateAsync(bool seed = false)
    {
        // The database exists for as long as one connection to it is open.
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddDbContext<SentinelDbContext>(o => o.UseSqlite(connection));
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<SentinelDbContext>());
        services.AddSingleton<IClock>(new FakeClock(Now));
        services.AddSingleton<IGeoLocator, StaticGeoLocator>();
        services.AddScoped<DevelopmentDataSeeder>();

        var host = new TestHost(connection, services.BuildServiceProvider(validateScopes: true));

        await using var scope = host._services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SentinelDbContext>();
        await db.Database.EnsureCreatedAsync();
        if (seed)
            await scope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>().SeedAsync();

        return host;
    }

    /// <summary>Sends a request in its own scope, like one HTTP request would.</summary>
    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request) where TResponse : Result
    {
        await using var scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IDispatcher>().Send(request);
    }

    /// <summary>A fresh context, so assertions read what was saved rather than what is cached.</summary>
    public async Task<T> Query<T>(Func<SentinelDbContext, Task<T>> query)
    {
        await using var scope = _services.CreateAsyncScope();
        return await query(scope.ServiceProvider.GetRequiredService<SentinelDbContext>());
    }

    public async Task<Guid> UserIdAsync(string email) =>
        await Query(db => db.Users.Where(u => u.Email == Domain.Users.EmailAddress.Create(email).Value).Select(u => u.Id).SingleAsync());

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
