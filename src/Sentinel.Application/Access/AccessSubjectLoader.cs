using Microsoft.EntityFrameworkCore;
using Sentinel.Application.Abstractions.Ports;
using Sentinel.Domain.Access;

namespace Sentinel.Application.Access;

/// <summary>
/// Loads what <see cref="PageAccessEvaluator"/> needs to know about a user.
/// Shared by every slice that answers "what may this person see?", which is
/// the one place sharing between slices is worth its coupling: the rule
/// "how a user becomes an AccessSubject" must not drift between screens.
/// </summary>
internal static class AccessSubjectLoader
{
    /// <returns>The subject, or null when <paramref name="userId"/> names no user.</returns>
    public static async Task<AccessSubject?> LoadAccessSubjectAsync(
        this IApplicationDbContext db, Guid? userId, CancellationToken cancellationToken)
    {
        if (userId is null)
            return AccessSubject.Anonymous;

        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return null;

        var roleIds = user.RoleIds.ToList();
        var roles = await db.Roles
            .AsNoTracking()
            .Where(r => roleIds.Contains(r.Id))
            .ToListAsync(cancellationToken);

        return AccessSubject.For(user, roles);
    }
}
