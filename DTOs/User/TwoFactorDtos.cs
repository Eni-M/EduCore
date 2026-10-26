namespace EduCore.Dtos.User;

// The answer to POST /auth/login. Exactly one half is set: a session when the password was
// enough, or a challenge id when a second step is owed. The code itself is never in here --
// it goes to the account's mailbox and nowhere else.
public record LoginResultDto(
    AuthResultDto? Session, Guid? TwoFactorChallengeId, bool ViaAuthenticator = false, bool Passkey = false)
{
    public static LoginResultDto SignedIn(AuthResultDto session) => new(session, null);
    public static LoginResultDto NeedsSecondStep(Guid challengeId, bool viaAuthenticator, bool passkey) =>
        new(null, challengeId, viaAuthenticator, passkey);
}

// What the client gets when a second step is owed. The challenge id is a handle, not a secret;
// it is useless without the code.
// Authenticator: the code comes from the user's app, not their inbox, so the screen says so.
// Passkey: the account has one, so the screen can offer it (and no code was emailed).
public record TwoFactorRequiredDto(
    Guid ChallengeId, bool TwoFactorRequired = true, bool Authenticator = false, bool Passkey = false);

public record PasskeyRegistrationDto(string Name, Fido2NetLib.AuthenticatorAttestationRawResponse Response);

public record PasskeySignInDto(Guid ChallengeId, Fido2NetLib.AuthenticatorAssertionRawResponse Response);

public record PasskeyChallengeDto(Guid ChallengeId);

public record EmailCodeInsteadDto(Guid ChallengeId);

public record AuthenticatorSetupDto(string Secret, string OtpAuthUri);

public record AuthenticatorCodeDto(string Code);

public record RemoveAuthenticatorDto(string CurrentPassword);

public record BeginAuthenticatorDto(string CurrentPassword);

public record VerifyTwoFactorDto(Guid ChallengeId, string Code);

// CurrentPassword is required only to switch the second step off.
public record TwoFactorPreferenceDto(bool Enabled, string? CurrentPassword = null);

// What the client needs to render the toggle: whether it is on, and whether the user is even
// allowed to turn it off.
public record TwoFactorStatusDto(bool Enabled, bool Required, bool Authenticator = false);
