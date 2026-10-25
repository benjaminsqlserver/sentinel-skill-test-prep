using Sentinel.Domain.Security;

namespace Sentinel.Domain.Tests.Security;

public class GeoLocationTests
{
    [Fact]
    public void Distance_between_Lagos_and_London_is_about_five_thousand_km()
    {
        var km = Places.Lagos.DistanceKmTo(Places.London);

        Assert.NotNull(km);
        Assert.InRange(km.Value, 5_000, 5_200);
    }

    [Fact]
    public void Distance_is_symmetric_and_zero_to_itself()
    {
        Assert.Equal(Places.Lagos.DistanceKmTo(Places.London)!.Value, Places.London.DistanceKmTo(Places.Lagos)!.Value, precision: 6);
        Assert.Equal(0, Places.Lagos.DistanceKmTo(Places.Lagos)!.Value, precision: 6);
    }

    [Fact]
    public void Distance_to_an_unknown_location_is_unknown()
    {
        Assert.Null(Places.Lagos.DistanceKmTo(GeoLocation.Unknown));
    }

    [Theory]
    [InlineData("N", 0, 0)]
    [InlineData("NG", 91, 0)]
    [InlineData("NG", 0, -181)]
    public void Invalid_locations_are_rejected(string country, double lat, double lon)
    {
        Assert.ThrowsAny<ArgumentException>(() => GeoLocation.Create(country, "X", lat, lon));
    }
}

public class SignInAttemptTests
{
    private static readonly DateTime Now = History.Now;

    [Fact]
    public void Non_utc_timestamps_are_rejected()
    {
        var result = SignInAttempt.Record(Guid.NewGuid(), DateTime.SpecifyKind(Now, DateTimeKind.Unspecified),
            "102.89.34.1", Places.Lagos, Agents.Laptop, succeeded: true);

        Assert.Equal(SignInAttemptErrors.TimestampNotUtc, result.Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-ip")]
    [InlineData("300.1.1.1")]
    public void Invalid_ip_addresses_are_rejected(string ip)
    {
        var result = SignInAttempt.Record(Guid.NewGuid(), Now, ip, Places.Lagos, Agents.Laptop, succeeded: true);

        Assert.Equal("SignIn.InvalidIpAddress", result.Error!.Code);
    }

    [Fact]
    public void A_failure_must_have_a_reason_and_a_success_must_not()
    {
        Assert.True(SignInAttempt.Record(Guid.NewGuid(), Now, "::1", Places.Lagos, null, succeeded: false).IsFailure);
        Assert.True(SignInAttempt.Record(Guid.NewGuid(), Now, "::1", Places.Lagos, null, succeeded: true, SignInFailureReason.WrongPassword).IsFailure);
    }

    [Fact]
    public void Device_id_is_the_same_for_the_same_agent_regardless_of_case_and_spacing()
    {
        Assert.Equal(DeviceFingerprint.FromUserAgent(Agents.Laptop), DeviceFingerprint.FromUserAgent("  " + Agents.Laptop.ToUpperInvariant()));
        Assert.NotEqual(DeviceFingerprint.FromUserAgent(Agents.Laptop), DeviceFingerprint.FromUserAgent(Agents.Phone));
    }

    [Theory]
    [InlineData(Agents.Laptop, "Chrome on Windows")]
    [InlineData(Agents.Phone, "Safari on iPhone")]
    [InlineData("python-requests/2.32.3", "python-requests")]
    [InlineData("", "unknown browser")]
    public void Agents_are_described_in_words(string agent, string expected)
    {
        Assert.Equal(expected, DeviceFingerprint.Describe(agent));
    }
}
