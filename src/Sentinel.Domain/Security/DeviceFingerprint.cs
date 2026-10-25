using System.Security.Cryptography;
using System.Text;

namespace Sentinel.Domain.Security;

/// <summary>
/// A short, stable identifier for "the same browser on the same machine".
/// Real products combine many signals; we hash the user agent, which is
/// enough to demonstrate the rule and honest about being weak evidence.
/// </summary>
public static class DeviceFingerprint
{
    public static string FromUserAgent(string? userAgent)
    {
        var normalised = (userAgent ?? string.Empty).Trim().ToLowerInvariant();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalised));
        return Convert.ToHexStringLower(hash)[..16];
    }

    /// <summary>Just enough of a user agent to be recognisable in a sentence.</summary>
    public static string Describe(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return "unknown browser";

        string[] known = ["Edg", "Chrome", "Firefox", "Safari", "curl", "python-requests"];
        var browser = known.FirstOrDefault(k => userAgent.Contains(k, StringComparison.OrdinalIgnoreCase));
        var os = userAgent.Contains("Windows", StringComparison.OrdinalIgnoreCase) ? "Windows"
               : userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase) ? "Android"
               : userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase) ? "iPhone"
               : userAgent.Contains("Mac OS", StringComparison.OrdinalIgnoreCase) ? "macOS"
               : userAgent.Contains("Linux", StringComparison.OrdinalIgnoreCase) ? "Linux"
               : null;

        return (browser == "Edg" ? "Edge" : browser, os) switch
        {
            (null, null) => "unrecognised client",
            (null, { } o) => o,
            ({ } b, null) => b,
            ({ } b, { } o) => $"{b} on {o}"
        };
    }
}
