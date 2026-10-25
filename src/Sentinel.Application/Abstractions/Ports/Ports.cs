using Microsoft.EntityFrameworkCore;
using Sentinel.Domain.Pages;
using Sentinel.Domain.Roles;
using Sentinel.Domain.Security;
using Sentinel.Domain.Users;

namespace Sentinel.Application.Abstractions.Ports;

/// <summary>
/// The persistence port. Infrastructure implements it with EF Core; handlers
/// use it with LINQ. There is no repository layer on top (Chapter 3 explains
/// why), and this interface is the reason none is needed: it is owned by
/// Application, so the dependency rule holds.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Page> Pages { get; }
    DbSet<SignInAttempt> SignInAttempts { get; }
    DbSet<FindingAcknowledgement> FindingAcknowledgements { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// The current time, injectable. A handler that calls <c>DateTime.UtcNow</c>
/// directly cannot be tested at "thirty-one days later".
/// </summary>
public interface IClock
{
    DateTime UtcNow { get; }
}

/// <summary>Resolves an IP address to an approximate location.</summary>
public interface IGeoLocator
{
    GeoLocation Locate(string ipAddress);
}
