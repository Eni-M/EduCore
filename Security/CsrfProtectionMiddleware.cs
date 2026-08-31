using System.Security.Cryptography;
using System.Text;
using EduCore.Exceptions;

namespace EduCore.Security;

public class CsrfProtectionMiddleware(RequestDelegate next, ISessionCookies cookies)
{
    private static readonly HashSet<string> SafeMethods =
        new(StringComparer.OrdinalIgnoreCase) { "GET", "HEAD", "OPTIONS", "TRACE" };

    public async Task InvokeAsync(HttpContext context)
    {
        if (RequiresCheck(context) && !HasMatchingToken(context))
            throw new ForbiddenException(
                "This request is missing a valid CSRF token. Send the value of the " +
                $"{cookies.CsrfCookieName} cookie in the {cookies.CsrfHeaderName} header.");

        await next(context);
    }

    private bool RequiresCheck(HttpContext context)
    {
        if (SafeMethods.Contains(context.Request.Method))
            return false;

        return context.Request.Cookies.ContainsKey(cookies.CookieName);
    }

    private bool HasMatchingToken(HttpContext context)
    {
        var fromCookie = context.Request.Cookies[cookies.CsrfCookieName];
        var fromHeader = context.Request.Headers[cookies.CsrfHeaderName].ToString();

        if (string.IsNullOrEmpty(fromCookie) || string.IsNullOrEmpty(fromHeader))
            return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(fromCookie),
            Encoding.UTF8.GetBytes(fromHeader));
    }
}
