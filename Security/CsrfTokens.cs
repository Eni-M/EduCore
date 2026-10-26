using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;

namespace EduCore.Security;

// The CSRF token is an HMAC of the session's token id (jti), not a random value merely copied into
// a cookie and a header. A plain double-submit token can be beaten by anyone who can write a cookie
// on the domain (a sibling subdomain, a network position on http): they set both halves to a value
// they chose. A token bound to the session cannot be minted without the key, and it dies with the
// session it was issued for. OWASP's "signed double-submit cookie" pattern.
public sealed class CsrfTokens(byte[] key)
{
    // The key is derived from Jwt:Key for its own purpose, so the signing key is never used raw.
    public static CsrfTokens FromJwtKey(string jwtKey) =>
        new(HKDF.DeriveKey(HashAlgorithmName.SHA256, Encoding.UTF8.GetBytes(jwtKey), 32,
            salt: null, info: "EduCore.Csrf.v1"u8.ToArray()));

    public string For(string sessionToken) => Compute(JwtId(sessionToken));

    // The session token is read, not validated, here: authentication validates it later in the
    // pipeline and rejects a forged one. All this needs is the id the CSRF token was bound to.
    public bool Matches(string sessionToken, string presented)
    {
        var jti = JwtId(sessionToken);
        if (jti is null || string.IsNullOrEmpty(presented))
            return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(Compute(jti)), Encoding.UTF8.GetBytes(presented));
    }

    private string Compute(string? jti) =>
        Base64UrlEncode(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(jti ?? "")));

    private static string? JwtId(string sessionToken)
    {
        var handler = new JwtSecurityTokenHandler();
        return handler.CanReadToken(sessionToken) ? handler.ReadJwtToken(sessionToken).Id : null;
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
