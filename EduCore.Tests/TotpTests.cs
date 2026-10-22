using System.Text;
using EduCore.Security;
using Xunit;

namespace EduCore.Tests;

public class TotpTests
{
    // RFC 6238 Appendix B, SHA-1 key "12345678901234567890". The RFC's codes are eight digits;
    // authenticator apps show the last six.
    private static readonly byte[] RfcKey = Encoding.ASCII.GetBytes("12345678901234567890");

    [Theory]
    [InlineData(59L, "287082")]
    [InlineData(1111111109L, "081804")]
    [InlineData(1111111111L, "050471")]
    [InlineData(1234567890L, "005924")]
    [InlineData(2000000000L, "279037")]
    public void Matches_the_RFC_6238_test_vectors(long unixTime, string expected) =>
        Assert.Equal(expected, Totp.Code(RfcKey, unixTime / 30));

    [Fact]
    public void Accepts_the_current_code_and_one_step_of_clock_drift()
    {
        var now = DateTimeOffset.FromUnixTimeSeconds(1111111109);
        var step = now.ToUnixTimeSeconds() / 30;

        Assert.Equal(step, Totp.Verify(RfcKey, Totp.Code(RfcKey, step), now, null));
        Assert.Equal(step - 1, Totp.Verify(RfcKey, Totp.Code(RfcKey, step - 1), now, null));
        Assert.Null(Totp.Verify(RfcKey, Totp.Code(RfcKey, step - 3), now, null));
    }

    // A code seen over someone's shoulder cannot be used again once it has been used.
    [Fact]
    public void Refuses_a_code_from_a_step_already_used()
    {
        var now = DateTimeOffset.FromUnixTimeSeconds(1111111109);
        var step = now.ToUnixTimeSeconds() / 30;

        Assert.Null(Totp.Verify(RfcKey, Totp.Code(RfcKey, step), now, lastUsedStep: step));
    }

    [Fact]
    public void Base32_round_trips_a_secret()
    {
        var secret = Totp.NewSecret();
        Assert.Equal(secret, Totp.FromBase32(Totp.ToBase32(secret)));
        Assert.Equal("GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ", Totp.ToBase32(RfcKey));
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("12a456")]
    public void Refuses_malformed_codes(string code) =>
        Assert.Null(Totp.Verify(RfcKey, code, DateTimeOffset.UtcNow, null));
}
