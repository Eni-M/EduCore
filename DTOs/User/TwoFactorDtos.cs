namespace EduCore.Dtos.User;

// The answer to POST /auth/login. Exactly one half is set: a session when the password was
// enough, or a challenge id when a second step is owed. The code itself is never in here --
// it goes to the account's mailbox and nowhere else.
public record LoginResultDto(AuthResultDto? Session, Guid? TwoFactorChallengeId, bool ViaAuthenticator = false)
{
    public static LoginResultDto SignedIn(AuthResultDto session) => new(session, null);
    public static LoginResultDto NeedsSecondStep(Guid challengeId, bool viaAuthenticator) =>
        new(null, challengeId, viaAuthenticator);
}

// What the client gets when a second step is owed. The challenge id is a handle, not a secret;
// it is useless without the code.
// Authenticator: the code comes from the user's app, not their inbox, so the screen says so.
public record TwoFactorRequiredDto(Guid ChallengeId, bool TwoFactorRequired = true, bool Authenticator = false);

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
