using EduCore.Data;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class PasswordResetTokenRepository(AppDbContext context)
    : Repository<PasswordResetToken>(context), IPasswordResetTokenRepository
{
    public async Task<PasswordResetToken?> GetByHashAsync(string tokenHash, CancellationToken ct) =>
        await _context.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task InvalidateOutstandingAsync(Guid userId, DateTime nowUtc, CancellationToken ct) =>
        await _context.PasswordResetTokens
            .Where(t => t.UserId == userId && t.UsedAt == null && t.ExpiresAt > nowUtc)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedAt, nowUtc), ct);
}
