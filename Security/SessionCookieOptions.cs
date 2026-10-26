using Microsoft.Extensions.Options;

namespace EduCore.Security;

public class SessionCookieOptions
{
    public const string SectionName = "Session";

    public string CookieName { get; set; } = "educore.session";

    public string CsrfCookieName { get; set; } = "educore.csrf";

    public string CsrfHeaderName { get; set; } = "X-CSRF-Token";

    public SameSiteMode SameSite { get; set; } = SameSiteMode.Lax;

    public bool Secure { get; set; } = true;

    public string? Domain { get; set; }

    // The name a cookie is actually set under. Over https, __Host- (no Domain, path /, Secure --
    // which browsers enforce) stops a sibling subdomain or a plain-http response from planting or
    // overwriting it; with a Domain set, __Host- is not allowed, so __Secure-. Over plain http (the
    // local dev loop) neither prefix is accepted by browsers, so the bare name.
    public string Effective(string name) =>
        !Secure ? name : Domain is null ? $"__Host-{name}" : $"__Secure-{name}";
}

public class SessionCookieOptionsValidator : IValidateOptions<SessionCookieOptions>
{
    public ValidateOptionsResult Validate(string? name, SessionCookieOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.CookieName))
            failures.Add("Session:CookieName must not be empty.");

        if (string.IsNullOrWhiteSpace(options.CsrfCookieName))
            failures.Add("Session:CsrfCookieName must not be empty.");

        if (string.IsNullOrWhiteSpace(options.CsrfHeaderName))
            failures.Add("Session:CsrfHeaderName must not be empty.");

        if (options.CookieName == options.CsrfCookieName)
            failures.Add("Session:CookieName and Session:CsrfCookieName must differ.");

        if (options.SameSite == SameSiteMode.None && !options.Secure)
            failures.Add(
                "Session:SameSite=None requires Session:Secure=true. A browser rejects a " +
                "SameSite=None cookie that is not marked Secure, so sign-in would silently fail.");

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
