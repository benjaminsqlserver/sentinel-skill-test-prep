using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Sentinel.Infrastructure.Persistence;

/// <summary>
/// Lets <c>dotnet ef migrations add</c> build a context without starting the
/// API. The connection string is only used for commands that touch a real
/// database (<c>database update</c>); adding a migration never connects.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SentinelDbContext>
{
    public SentinelDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("SENTINEL_CONNECTION")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=Sentinel;Trusted_Connection=True;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<SentinelDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new SentinelDbContext(options);
    }
}
