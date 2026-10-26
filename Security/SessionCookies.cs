using Microsoft.Extensions.Options;

namespace EduCore.Security;

public class SessionCookies(IOptions<SessionCookieOptions> options, CsrfTokens csrf) : ISessionCookies
{
    private readonly SessionCookieOptions settings = options.Value;

    public string CookieName => settings.Effective(settings.CookieName);

    public string CsrfCookieName => settings.Effective(settings.CsrfCookieName);

    public string CsrfHeaderName => settings.CsrfHeaderName;

    public void Issue(HttpResponse response, string token, DateTime expiresAtUtc)
    {
        response.Cookies.Append(CookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = settings.Secure,
            SameSite = settings.SameSite,
            Domain = settings.Domain,
            Path = "/",
            Expires = new DateTimeOffset(DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc))
        });

        // Bound to this session (see CsrfTokens); readable by the page, which echoes it in a header.
        response.Cookies.Append(CsrfCookieName, csrf.For(token), new CookieOptions
        {
            HttpOnly = false,
            Secure = settings.Secure,
            SameSite = settings.SameSite,
            Domain = settings.Domain,
            Path = "/",
            Expires = new DateTimeOffset(DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc))
        });
    }

    public void Clear(HttpResponse response)
    {
        var expired = new CookieOptions
        {
            Secure = settings.Secure,
            SameSite = settings.SameSite,
            Domain = settings.Domain,
            Path = "/"
        };

        response.Cookies.Delete(CookieName, expired);
        response.Cookies.Delete(CsrfCookieName, expired);
    }
}
