using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface ITwoFactorChallengeRepository : IRepository<TwoFactorChallenge>
{
    // Resolved by id rather than by code: the caller presents both, and looking up by the
    // non-secret half means the code is only ever compared, never used to search.
    Task<TwoFactorChallenge?> GetOpenAsync(Guid challengeId, CancellationToken ct);

    // Retires a user's outstanding challenges for one purpose. Called when a new code is
    // issued, so exactly one is ever live and an older email stops working.
    Task InvalidateOutstandingAsync(Guid userId, TwoFactorPurpose purpose, DateTime nowUtc, CancellationToken ct);
}
