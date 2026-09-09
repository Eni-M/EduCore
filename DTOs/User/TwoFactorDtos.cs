namespace EduCore.Dtos.User;

// The answer to POST /auth/login. Exactly one half is set: a session when the password was
// enough, or a challenge id when a second step is owed. The code itself is never in here --
// it goes to the account's mailbox and nowhere else.
public record LoginResultDto(AuthResultDto? Session, Guid? TwoFactorChallengeId)
{
    public static LoginResultDto SignedIn(AuthResultDto session) => new(session, null);
    public static LoginResultDto NeedsSecondStep(Guid challengeId) => new(null, challengeId);
}

// What the client gets when a second step is owed. The challenge id is a handle, not a secret;
// it is useless without the code.
public record TwoFactorRequiredDto(Guid ChallengeId, bool TwoFactorRequired = true);

public record VerifyTwoFactorDto(Guid ChallengeId, string Code);

public record TwoFactorPreferenceDto(bool Enabled);

// What the client needs to render the toggle: whether it is on, and whether the user is even
// allowed to turn it off.
public record TwoFactorStatusDto(bool Enabled, bool Required);
