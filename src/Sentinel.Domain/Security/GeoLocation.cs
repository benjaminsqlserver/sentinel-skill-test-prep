namespace Sentinel.Domain.Security;

/// <summary>
/// Where a sign-in appeared to come from, as resolved from its IP address.
/// IP geolocation is approximate: city-level at best, and wrong for VPNs.
/// Treat it as evidence, never as proof.
/// </summary>
public sealed record GeoLocation
{
    public const string UnknownCountryCode = "ZZ"; // ISO 3166 user-assigned "unknown"
    private const double EarthRadiusKm = 6371.0;

    private GeoLocation(string countryCode, string city, double? latitude, double? longitude)
    {
        CountryCode = countryCode;
        City = city;
        Latitude = latitude;
        Longitude = longitude;
    }

    public string CountryCode { get; }
    public string City { get; }
    public double? Latitude { get; }
    public double? Longitude { get; }

    public bool IsKnown => CountryCode != UnknownCountryCode;
    public bool HasCoordinates => Latitude.HasValue && Longitude.HasValue;

    public static GeoLocation Unknown { get; } = new(UnknownCountryCode, "Unknown", null, null);

    public static GeoLocation Create(string countryCode, string city, double latitude, double longitude)
    {
        if (string.IsNullOrWhiteSpace(countryCode) || countryCode.Trim().Length != 2)
            throw new ArgumentException("Country code must be ISO 3166-1 alpha-2.", nameof(countryCode));
        if (latitude is < -90 or > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude));
        if (longitude is < -180 or > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude));

        return new GeoLocation(countryCode.Trim().ToUpperInvariant(), city.Trim(), latitude, longitude);
    }

    /// <summary>Rehydrates a stored location. Null coordinates mean "unknown".</summary>
    public static GeoLocation Restore(string countryCode, string city, double? latitude, double? longitude) =>
        countryCode == UnknownCountryCode || latitude is null || longitude is null
            ? Unknown
            : Create(countryCode, city, latitude.Value, longitude.Value);

    /// <summary>
    /// Great-circle distance by the haversine formula, or null when either
    /// end has no coordinates. Accurate to about 0.5%, which is far better
    /// than the IP lookup that produced the coordinates.
    /// </summary>
    public double? DistanceKmTo(GeoLocation other)
    {
        if (!HasCoordinates || !other.HasCoordinates)
            return null;

        var lat1 = ToRadians(Latitude!.Value);
        var lat2 = ToRadians(other.Latitude!.Value);
        var dLat = lat2 - lat1;
        var dLon = ToRadians(other.Longitude!.Value - Longitude!.Value);

        var a = Math.Pow(Math.Sin(dLat / 2), 2) +
                Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(dLon / 2), 2);

        return 2 * EarthRadiusKm * Math.Asin(Math.Sqrt(a));
    }

    public override string ToString() => IsKnown ? $"{City}, {CountryCode}" : "Unknown location";

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
