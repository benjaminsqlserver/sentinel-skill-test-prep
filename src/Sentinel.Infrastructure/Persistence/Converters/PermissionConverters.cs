using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Sentinel.Domain.Access;

namespace Sentinel.Infrastructure.Persistence.Converters;

internal sealed class PermissionConverter() : ValueConverter<Permission, string>(
    permission => permission.Value,
    value => Permission.From(value));

/// <summary>
/// Stores a role's permissions as one space-separated column. A space can
/// never appear inside a permission (the pattern forbids it), so splitting is
/// unambiguous. A child table would be more "relational", but nobody ever
/// queries "which roles grant X" in SQL; they ask the application.
/// </summary>
internal sealed class PermissionListConverter() : ValueConverter<List<Permission>, string>(
    permissions => string.Join(' ', permissions.Select(p => p.Value)),
    value => value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(Permission.From).ToList());

/// <summary>
/// Without a comparer EF Core compares the list by reference and never
/// notices that <c>Grant</c> added an item, so the change is silently lost.
/// </summary>
internal sealed class PermissionListComparer() : ValueComparer<List<Permission>>(
    (left, right) => left!.SequenceEqual(right!),
    list => list.Aggregate(0, (hash, p) => HashCode.Combine(hash, p.GetHashCode())),
    list => list.ToList());
