using Microsoft.EntityFrameworkCore;
using Sentinel.Application.Abstractions.Ports;
using Sentinel.Domain.Pages;
using Sentinel.Domain.Roles;
using Sentinel.Domain.Security;
using Sentinel.Domain.Users;
using Sentinel.Infrastructure.Persistence.Converters;

namespace Sentinel.Infrastructure.Persistence;

public sealed class SentinelDbContext(DbContextOptions<SentinelDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Page> Pages => Set<Page>();
    public DbSet<SignInAttempt> SignInAttempts => Set<SignInAttempt>();
    public DbSet<FindingAcknowledgement> FindingAcknowledgements => Set<FindingAcknowledgement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SentinelDbContext).Assembly);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Every DateTime in this model is UTC. SQL Server's datetime2 has no
        // notion of a zone, so without this line every value read back has
        // DateTimeKind.Unspecified, serialises without its 'Z', and is then
        // read by every browser on earth as local time. Chapter 22 is the
        // post-mortem of deleting this line.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }
}
