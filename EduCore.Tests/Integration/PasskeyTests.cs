using System.Formats.Cbor;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using EduCore.Enums;
using EduCore.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace EduCore.Tests.Integration;

// A passkey added and then used to finish a sign-in, end to end through Fido2NetLib, with a small
// software authenticator standing in for the device: a P-256 key, "none" attestation, signatures
// over the bytes WebAuthn defines. No email code is sent once the account has a passkey.
[Collection(ApiCollection.Name)]
public sealed class PasskeyTests(ApiFixture api)
{
    private const string Origin = "http://localhost:5173";
    private const string Password = "correct horse battery staple";

    [Fact]
    public async Task A_registered_passkey_finishes_the_sign_in()
    {
        var ct = TestContext.Current.CancellationToken;
        var user = await UserWithPasswordAsync();
        using var device = new SoftwareAuthenticator();
        var client = api.App.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiFixture.Token(user));

        // Adding one needs the password.
        var refused = await client.PostAsJsonAsync("/api/auth/me/passkeys/options", new { currentPassword = "wrong" }, ct);
        Assert.NotEqual(HttpStatusCode.OK, refused.StatusCode);

        var creation = await Json(await client.PostAsJsonAsync(
            "/api/auth/me/passkeys/options", new { currentPassword = Password }, ct));
        var registered = await client.PostAsJsonAsync("/api/auth/me/passkeys", new
        {
            name = "Test laptop",
            response = device.Create(creation["challenge"]!.GetValue<string>()),
        }, ct);
        Assert.Equal(HttpStatusCode.OK, registered.StatusCode);

        // The password now leads to the passkey, not to an emailed code.
        var anonymous = api.App.CreateClient();
        var login = await Json(await anonymous.PostAsJsonAsync(
            "/api/auth/login", new { email = user.Email, password = Password }, ct));
        Assert.True(login["passkey"]!.GetValue<bool>());
        var challengeId = login["challengeId"]!.GetValue<Guid>();

        var request = await Json(await anonymous.PostAsJsonAsync(
            "/api/auth/login/passkey/options", new { challengeId }, ct));
        var challenge = request["challenge"]!.GetValue<string>();

        // A signature from some other key is refused.
        using var stranger = new SoftwareAuthenticator(device.CredentialId);
        var forged = await anonymous.PostAsJsonAsync("/api/auth/login/passkey",
            new { challengeId, response = stranger.Get(challenge, user.Id) }, ct);
        Assert.Equal(HttpStatusCode.Unauthorized, forged.StatusCode);

        // The real one, against fresh options (each set answers once).
        request = await Json(await anonymous.PostAsJsonAsync("/api/auth/login/passkey/options", new { challengeId }, ct));
        var signedIn = await anonymous.PostAsJsonAsync("/api/auth/login/passkey",
            new { challengeId, response = device.Get(request["challenge"]!.GetValue<string>(), user.Id) }, ct);
        Assert.Equal(HttpStatusCode.OK, signedIn.StatusCode);
        Assert.Contains(signedIn.Headers.GetValues("Set-Cookie"), c => c.StartsWith("educore.session="));
    }

    private async Task<User> UserWithPasswordAsync()
    {
        var seeded = await api.UserAsync(UserRole.Teacher);
        await using var db = api.Database();
        var user = await db.Users.SingleAsync(u => u.Id == seeded.Id);
        user.SetPassword(Password, new PasswordHasher<User>(Options.Create(new PasswordHasherOptions { IterationCount = 1 })));
        await db.SaveChangesAsync();
        return user;
    }

    private static async Task<JsonObject> Json(HttpResponseMessage response)
    {
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject();
    }

    private sealed class SoftwareAuthenticator(byte[]? credentialId = null) : IDisposable
    {
        private readonly ECDsa _key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        public byte[] CredentialId { get; } = credentialId ?? RandomNumberGenerator.GetBytes(16);

        private static readonly byte[] RpIdHash = SHA256.HashData("localhost"u8);

        public object Create(string challenge)
        {
            var clientData = ClientData("webauthn.create", challenge);
            var p = _key.ExportParameters(false);

            var cose = new CborWriter(CborConformanceMode.Ctap2Canonical);
            cose.WriteStartMap(5);
            cose.WriteInt32(1); cose.WriteInt32(2);   // kty: EC2
            cose.WriteInt32(3); cose.WriteInt32(-7);  // alg: ES256
            cose.WriteInt32(-1); cose.WriteInt32(1);  // crv: P-256
            cose.WriteInt32(-2); cose.WriteByteString(p.Q.X!);
            cose.WriteInt32(-3); cose.WriteByteString(p.Q.Y!);
            cose.WriteEndMap();

            byte[] authData =
            [
                .. RpIdHash, 0x45 /* UP | UV | AT */, 0, 0, 0, 0,
                .. new byte[16] /* AAGUID */, (byte)(CredentialId.Length >> 8), (byte)CredentialId.Length,
                .. CredentialId, .. cose.Encode(),
            ];

            var attestation = new CborWriter(CborConformanceMode.Ctap2Canonical);
            attestation.WriteStartMap(3);
            attestation.WriteTextString("fmt"); attestation.WriteTextString("none");
            attestation.WriteTextString("attStmt"); attestation.WriteStartMap(0); attestation.WriteEndMap();
            attestation.WriteTextString("authData"); attestation.WriteByteString(authData);
            attestation.WriteEndMap();

            return new
            {
                id = B64(CredentialId),
                rawId = B64(CredentialId),
                type = "public-key",
                response = new { attestationObject = B64(attestation.Encode()), clientDataJSON = B64(clientData) },
                clientExtensionResults = new { },
            };
        }

        public object Get(string challenge, Guid userId)
        {
            var clientData = ClientData("webauthn.get", challenge);
            byte[] authData = [.. RpIdHash, 0x05 /* UP | UV */, 0, 0, 0, 0];
            var signature = _key.SignData([.. authData, .. SHA256.HashData(clientData)],
                HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);

            return new
            {
                id = B64(CredentialId),
                rawId = B64(CredentialId),
                type = "public-key",
                response = new
                {
                    authenticatorData = B64(authData),
                    clientDataJSON = B64(clientData),
                    signature = B64(signature),
                    userHandle = B64(userId.ToByteArray()),
                },
                clientExtensionResults = new { },
            };
        }

        private static byte[] ClientData(string type, string challenge) =>
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { type, challenge, origin = Origin, crossOrigin = false }));

        private static string B64(byte[] bytes) =>
            Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        public void Dispose() => _key.Dispose();
    }
}
