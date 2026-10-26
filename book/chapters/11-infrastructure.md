# Chapter 11 · Infrastructure and Persistence

## 11.1 The DbContext

<!-- include: src/Sentinel.Infrastructure/Persistence/SentinelDbContext.cs -->

The context implements `IApplicationDbContext` and applies one
configuration class per aggregate. `ConfigureConventions` contains the most
consequential line in the project. SQL Server's `datetime2` stores an
instant with no notion of a zone. Without a converter, every `DateTime`
EF Core reads back has `DateTimeKind.Unspecified`, `System.Text.Json`
serialises it without a trailing `Z`, and every browser on earth then
reads it as *local* time. One line prevents that everywhere:

<!-- include: src/Sentinel.Infrastructure/Persistence/Converters/UtcDateTimeConverter.cs -->

It also refuses to *write* a non-UTC `DateTime`, so a local time cannot
sneak into the database. (Converters are expression trees, which cannot
contain `throw` expressions; hence the helper method.)

## 11.2 Configurations

<!-- include: src/Sentinel.Infrastructure/Persistence/Configurations/UserConfiguration.cs -->

Roles and overrides are *owned* collections: they have no life outside a
user, so they have no `DbSet` of their own. Navigations are mapped to the
private fields so that EF Core can populate them while the public API stays
read-only.

<!-- include: src/Sentinel.Infrastructure/Persistence/Converters/PermissionConverters.cs -->

<!-- include: src/Sentinel.Infrastructure/Persistence/Configurations/RoleConfiguration.cs -->

The value comparer is not optional. Without it EF Core compares the list by
reference, never notices that `Grant` added to it, and silently does not
save. The test `Granting_a_role_permission_is_saved_and_changes_access`
(Chapter 13) exists because this is exactly the bug you get.

<!-- include: src/Sentinel.Infrastructure/Persistence/Configurations/PageConfiguration.cs -->

<!-- include: src/Sentinel.Infrastructure/Persistence/Configurations/SecurityConfigurations.cs -->

The composite index on `(UserId, OccurredAtUtc DESC)` matches the one query
that matters: "this user's attempts, newest first".

## 11.3 Services

<!-- include: src/Sentinel.Infrastructure/Services/SystemClock.cs -->

`TimeProvider` is .NET's own clock abstraction (since .NET 8). Wrapping it
in `IClock` keeps the Application layer independent of even that.

<!-- include: src/Sentinel.Infrastructure/Services/StaticGeoLocator.cs -->

## 11.4 Seeding the stories

<!-- include: src/Sentinel.Infrastructure/Persistence/Seeding/DevelopmentDataSeeder.cs -->

Two details are worth stealing:

- **The seeder uses the domain factories**, so seed data obeys the same
  rules as real data.
- **Its random numbers are seeded from the email's characters**, not
  `string.GetHashCode()`, which in .NET is randomised per process. The
  first draft used `GetHashCode` and produced different demo data on every
  run, which is exactly the kind of non-determinism that turns a debugging
  session into a ghost hunt.

## 11.5 Registration and migrations

<!-- include: src/Sentinel.Infrastructure/DependencyInjection.cs -->

<!-- include: src/Sentinel.Infrastructure/Persistence/DesignTimeDbContextFactory.cs -->

```powershell
dotnet ef migrations add InitialCreate `
  --project src/Sentinel.Infrastructure --startup-project src/Sentinel.Infrastructure `
  --output-dir Persistence/Migrations
```

The API applies migrations and seeds on start-up in development
(Chapter 12). In production, apply migrations from the pipeline with
`dotnet ef migrations bundle`, never from the running application.
