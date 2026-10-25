using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sentinel.Application.Abstractions.Ports;
using Sentinel.Infrastructure.Persistence;
using Sentinel.Infrastructure.Persistence.Seeding;
using Sentinel.Infrastructure.Services;

namespace Sentinel.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "Sentinel";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is missing. Set ConnectionStrings:{ConnectionStringName} in appsettings or user secrets.");

        services.AddDbContext<SentinelDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

        // One context instance per request, reachable through the port.
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<SentinelDbContext>());

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IGeoLocator, StaticGeoLocator>();
        services.AddScoped<DevelopmentDataSeeder>();

        return services;
    }
}
