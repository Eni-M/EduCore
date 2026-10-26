using EduCore.Data;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class TwoFactorChallengeRepository(AppDbContext context)
    : Repository<TwoFactorChallenge>(context), ITwoFactorChallengeRepository
{
    public async Task<TwoFactorChallenge?> GetOpenAsync(Guid challengeId, CancellationToken ct) =>
        await _context.TwoFactorChallenges
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == challengeId && c.CompletedAt == null, ct);

    public async Task InvalidateOutstandingAsync(
        Guid userId, TwoFactorPurpose purpose, DateTime nowUtc, CancellationToken ct) =>
        await _context.TwoFactorChallenges
            .Where(c => c.UserId == userId && c.Purpose == purpose && c.CompletedAt == null)
            .ExecuteUpdateAsync(c => c.SetProperty(x => x.CompletedAt, nowUtc), ct);

    public async Task<bool> TryTakeAttemptAsync(
        Guid challengeId, int maxAttempts, DateTime nowUtc, CancellationToken ct) =>
        await _context.TwoFactorChallenges
            .Where(c => c.Id == challengeId && c.CompletedAt == null && c.ExpiresAt > nowUtc
                        && c.FailedAttempts < maxAttempts)
            .ExecuteUpdateAsync(c => c.SetProperty(x => x.FailedAttempts, x => x.FailedAttempts + 1), ct) == 1;

    public async Task<bool> TryCompleteAsync(Guid challengeId, DateTime nowUtc, CancellationToken ct) =>
        await _context.TwoFactorChallenges
            .Where(c => c.Id == challengeId && c.CompletedAt == null)
            .ExecuteUpdateAsync(c => c.SetProperty(x => x.CompletedAt, nowUtc), ct) == 1;
}
