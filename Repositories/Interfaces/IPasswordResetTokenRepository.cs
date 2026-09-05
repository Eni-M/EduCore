using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface IPasswordResetTokenRepository : IRepository<PasswordResetToken>
{
    // Resolves a presented token by its hash, with the owning user attached so redemption
    // needs no second round trip.
    Task<PasswordResetToken?> GetByHashAsync(string tokenHash, CancellationToken ct);

    // Marks every outstanding token for a user as used. Called when a new one is issued and
    // again on redemption, so exactly one link is ever live per account.
    Task InvalidateOutstandingAsync(Guid userId, DateTime nowUtc, CancellationToken ct);
}
