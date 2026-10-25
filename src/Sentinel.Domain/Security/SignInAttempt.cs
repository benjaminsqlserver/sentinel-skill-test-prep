using Sentinel.Domain.Common;

namespace Sentinel.Domain.Security;

public enum SignInFailureReason
{
    WrongPassword,
    WrongSecondFactor,
    AccountLocked,
    AccountSuspended
}

/// <summary>
/// One attempt to sign in, successful or not. Attempts are immutable facts:
/// once recorded they are never edited, only read.
/// </summary>
public sealed class SignInAttempt : AggregateRoot
{
    public const int MaxUserAgentLength = 512;

    private SignInAttempt(
        Guid id, Guid userId, DateTime occurredAtUtc, string ipAddress, GeoLocation location,
        string userAgent, bool succeeded, SignInFailureReason? failureReason) : base(id)
    {
        UserId = userId;
        OccurredAtUtc = occurredAtUtc;
        IpAddress = ipAddress;
        CountryCode = location.CountryCode;
        City = location.City;
        Latitude = location.Latitude;
        Longitude = location.Longitude;
        UserAgent = userAgent;
        DeviceId = DeviceFingerprint.FromUserAgent(userAgent);
        Succeeded = succeeded;
        FailureReason = failureReason;
    }

    // For EF Core.
    private SignInAttempt() { }

    public Guid UserId { get; private set; }

    /// <summary>
    /// Always <see cref="DateTimeKind.Utc"/>. Infrastructure owns the promise
    /// that this survives a round trip through the database; see
    /// <c>UtcDateTimeConverter</c> and Chapter 22 for what happens when it does not.
    /// </summary>
    public DateTime OccurredAtUtc { get; private set; }

    public string IpAddress { get; private set; } = null!;

    // Stored flat so that EF Core maps four plain columns. The value object
    // is rebuilt on read; it has no identity, so nothing is lost.
    public string CountryCode { get; private set; } = null!;
    public string City { get; private set; } = null!;
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }

    public GeoLocation Location => GeoLocation.Restore(CountryCode, City, Latitude, Longitude);

    public string UserAgent { get; private set; } = null!;
    public string DeviceId { get; private set; } = null!;
    public bool Succeeded { get; private set; }
    public SignInFailureReason? FailureReason { get; private set; }

    public static Result<SignInAttempt> Record(
        Guid userId,
        DateTime occurredAtUtc,
        string ipAddress,
        GeoLocation location,
        string? userAgent,
        bool succeeded,
        SignInFailureReason? failureReason = null,
        Guid? id = null)
    {
        ArgumentNullException.ThrowIfNull(location);

        if (occurredAtUtc.Kind != DateTimeKind.Utc)
            return SignInAttemptErrors.TimestampNotUtc;

        if (string.IsNullOrWhiteSpace(ipAddress) || !System.Net.IPAddress.TryParse(ipAddress.Trim(), out _))
            return SignInAttemptErrors.InvalidIpAddress(ipAddress);

        if (succeeded && failureReason is not null)
            return SignInAttemptErrors.SuccessWithFailureReason;

        if (!succeeded && failureReason is null)
            return SignInAttemptErrors.FailureWithoutReason;

        var agent = (userAgent ?? string.Empty).Trim();
        if (agent.Length > MaxUserAgentLength)
            agent = agent[..MaxUserAgentLength];

        return new SignInAttempt(
            id ?? Guid.CreateVersion7(), userId, occurredAtUtc, ipAddress.Trim(), location,
            agent, succeeded, failureReason);
    }
}

public static class SignInAttemptErrors
{
    public static readonly Error TimestampNotUtc =
        Error.Validation("SignIn.TimestampNotUtc", "Sign-in timestamps must be UTC (end the value with 'Z' or an offset).");

    public static Error InvalidIpAddress(string? ip) =>
        Error.Validation("SignIn.InvalidIpAddress", $"'{ip}' is not a valid IP address.");

    public static readonly Error SuccessWithFailureReason =
        Error.Validation("SignIn.SuccessWithFailureReason", "A successful sign-in cannot have a failure reason.");

    public static readonly Error FailureWithoutReason =
        Error.Validation("SignIn.FailureWithoutReason", "A failed sign-in must say why it failed.");
}
