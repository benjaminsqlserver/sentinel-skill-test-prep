using System.Net;
using Sentinel.Application.Abstractions.Ports;
using Sentinel.Domain.Security;

namespace Sentinel.Infrastructure.Services;

/// <summary>
/// A stand-in for a real geolocation database (MaxMind GeoLite2, ipinfo...).
/// The prefixes are illustrative: they make the demo data tell a story, and
/// they are not a claim about who owns those addresses.
/// </summary>
internal sealed class StaticGeoLocator : IGeoLocator
{
    private static readonly (string Prefix, GeoLocation Location)[] Table =
    [
        ("102.89.", GeoLocation.Create("NG", "Lagos", 6.5244, 3.3792)),
        ("105.112.", GeoLocation.Create("NG", "Abuja", 9.0765, 7.3986)),
        ("81.2.", GeoLocation.Create("GB", "London", 51.5072, -0.1276)),
        ("185.220.", GeoLocation.Create("DE", "Frankfurt", 50.1109, 8.6821)),
        ("145.53.", GeoLocation.Create("NL", "Amsterdam", 52.3676, 4.9041)),
        ("116.88.", GeoLocation.Create("SG", "Singapore", 1.3521, 103.8198)),
        ("72.229.", GeoLocation.Create("US", "New York", 40.7128, -74.0060)),
        ("177.38.", GeoLocation.Create("BR", "São Paulo", -23.5558, -46.6396)),
    ];

    public GeoLocation Locate(string ipAddress)
    {
        if (!IPAddress.TryParse(ipAddress, out var parsed) || IsPrivateOrLoopback(parsed))
            return GeoLocation.Unknown;

        var text = parsed.ToString();
        foreach (var (prefix, location) in Table)
        {
            if (text.StartsWith(prefix, StringComparison.Ordinal))
                return location;
        }

        return GeoLocation.Unknown;
    }

    private static bool IsPrivateOrLoopback(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
            return true;

        var bytes = address.GetAddressBytes();
        return bytes.Length == 4 && (bytes[0] == 10 ||
                                     (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) ||
                                     (bytes[0] == 192 && bytes[1] == 168));
    }
}
