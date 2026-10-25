using Microsoft.EntityFrameworkCore;
using Sentinel.Application.Abstractions.Ports;
using Sentinel.Domain.Pages;
using Sentinel.Domain.Roles;
using Sentinel.Domain.Security;
using Sentinel.Domain.Users;

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
}
