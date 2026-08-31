namespace EduCore.Security;

public interface ISessionCookies
{
    string CookieName { get; }

    string CsrfCookieName { get; }

    string CsrfHeaderName { get; }

    void Issue(HttpResponse response, string token, DateTime expiresAtUtc);

    void Clear(HttpResponse response);
}
