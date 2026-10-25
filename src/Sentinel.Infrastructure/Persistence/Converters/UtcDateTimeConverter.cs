using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Sentinel.Infrastructure.Persistence.Converters;

/// <summary>
/// Writes DateTimes as they are (they are UTC by domain rule) and stamps
/// <see cref="DateTimeKind.Utc"/> on everything read back. The database
/// stores the instant; this converter restores the knowledge of what kind
/// of instant it is.
/// </summary>
internal sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    toDatabase => RequireUtc(toDatabase),
    fromDatabase => DateTime.SpecifyKind(fromDatabase, DateTimeKind.Utc))
{
    // A method, not an inline throw: converters are expression trees, and
    // expression trees cannot contain throw expressions.
    private static DateTime RequireUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc
            ? value
            : throw new InvalidOperationException($"Refusing to store a {value.Kind} DateTime; the model is UTC-only.");
}
