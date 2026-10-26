using EduCore.Exceptions;

namespace EduCore.Security;

public class CsrfProtectionMiddleware(RequestDelegate next, ISessionCookies cookies, CsrfTokens csrf)
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

    // The header must carry the token bound to the session in the session cookie. The CSRF cookie
    // is only how the page learns it; a cookie someone else planted does not match this session.
    private bool HasMatchingToken(HttpContext context)
    {
        var session = context.Request.Cookies[cookies.CookieName];
        var fromHeader = context.Request.Headers[cookies.CsrfHeaderName].ToString();

        return !string.IsNullOrEmpty(session) && csrf.Matches(session, fromHeader);
    }
}
