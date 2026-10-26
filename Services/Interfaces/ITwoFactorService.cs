using EduCore.Models;

namespace EduCore.Services.Interfaces;

public interface ITwoFactorService
{
    // Whether finishing a sign-in for this user needs a second step: always for the roles that
    // manage accounts, and for anybody who has switched it on.
    bool SignInRequiresSecondStep(User user);

    // Issues a challenge and emails the code. Returns the challenge id, which is not a secret --
    // the code is. Both are needed to complete it.
    Task<Guid> IssueAsync(User user, TwoFactorPurpose purpose, CancellationToken ct);

    // Consumes a challenge. Throws if it is unknown, expired, out of attempts, already used,
    // for a different purpose, not owned by ownerId (when given), or the code is wrong. Returns the
    // user it belonged to.
    Task<User> CompleteAsync(
        Guid challengeId, string code, TwoFactorPurpose purpose, CancellationToken ct, Guid? ownerId = null);
}
