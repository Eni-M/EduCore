using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace EduCore.Security;

public class SessionCookies(IOptions<SessionCookieOptions> options) : ISessionCookies
{
    private readonly SessionCookieOptions settings = options.Value;

    public string CookieName => settings.CookieName;

    public string CsrfCookieName => settings.CsrfCookieName;

    public string CsrfHeaderName => settings.CsrfHeaderName;

    public void Issue(HttpResponse response, string token, DateTime expiresAtUtc)
    {
        response.Cookies.Append(settings.CookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = settings.Secure,
            SameSite = settings.SameSite,
            Domain = settings.Domain,
            Path = "/",
            Expires = new DateTimeOffset(DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc))
        });

        response.Cookies.Append(settings.CsrfCookieName, NewCsrfToken(), new CookieOptions
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

        response.Cookies.Delete(settings.CookieName, expired);
        response.Cookies.Delete(settings.CsrfCookieName, expired);
    }

    private static string NewCsrfToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
}
