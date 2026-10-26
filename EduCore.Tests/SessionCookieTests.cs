using EduCore.Security;
using Xunit;

namespace EduCore.Tests;

public class SessionCookieTests
{
    // __Host- needs Secure, no Domain and path / -- the strongest binding a browser offers.
    [Fact]
    public void Over_https_without_a_domain_cookies_get_the_host_prefix() =>
        Assert.Equal("__Host-educore.session", new SessionCookieOptions { Secure = true }.Effective("educore.session"));

    // __Host- forbids a Domain attribute, so a configured Domain falls back to __Secure-.
    [Fact]
    public void With_a_domain_cookies_get_the_secure_prefix() =>
        Assert.Equal("__Secure-educore.csrf",
            new SessionCookieOptions { Secure = true, Domain = "educore.website" }.Effective("educore.csrf"));

    // Browsers refuse either prefix on a non-Secure cookie, which is what the http dev loop sets.
    [Fact]
    public void Over_plain_http_cookies_keep_their_bare_name() =>
        Assert.Equal("educore.session", new SessionCookieOptions { Secure = false }.Effective("educore.session"));

    [Fact]
    public void A_csrf_token_matches_only_the_session_it_was_minted_for()
    {
        var csrf = CsrfTokens.FromJwtKey("unit-test-signing-key-at-least-32-bytes-long");
        var mine = Integration.ApiFixture.Token(new Models.User { Id = Guid.NewGuid(), Role = Enums.UserRole.Teacher });
        var theirs = Integration.ApiFixture.Token(new Models.User { Id = Guid.NewGuid(), Role = Enums.UserRole.Teacher });

        Assert.True(csrf.Matches(mine, csrf.For(mine)));
        Assert.False(csrf.Matches(theirs, csrf.For(mine)));
        Assert.False(csrf.Matches("not-a-jwt", csrf.For(mine)));
    }
}
