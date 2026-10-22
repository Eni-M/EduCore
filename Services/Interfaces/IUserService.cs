using EduCore.Dtos.User;
using EduCore.Enums;
using EduCore.Models;

namespace EduCore.Services.Interfaces;

public interface IUserService
{
    // Either a session or a second step owed -- see LoginResultDto.
    Task<LoginResultDto> LoginAsync(LoginUserDto dto, CancellationToken ct);

    Task<AuthResultDto> CompleteSignInAsync(VerifyTwoFactorDto dto, CancellationToken ct);

    // Emails the code that ChangePasswordAsync will demand, and returns the challenge id the
    // caller has to send back with it. The id is a handle, not a secret.
    Task<Guid> RequestPasswordChangeCodeAsync(Guid userId, CancellationToken ct);

    Task<TwoFactorStatusDto> GetTwoFactorStatusAsync(Guid userId, CancellationToken ct);
    Task<TwoFactorStatusDto> SetTwoFactorAsync(Guid userId, bool enabled, string? currentPassword, CancellationToken ct);

    Task<AuthResultDto> ChangePasswordAsync(Guid userId, ChangePasswordDto dto, CancellationToken ct);
    Task<AuthResultDto> UpdateOwnCredentialsAsync(
        Guid userId, string firstName, string lastName, string email, string? currentPassword,
        CancellationToken ct);
    Task<AuthResponseDto> GetCurrentAsync(Guid userId, CancellationToken ct);

    Task<ResetPasswordResponseDto> AdminResetPasswordAsync(Guid userId, CancellationToken ct);
    Task SetDigestPreferenceAsync(bool optOut, CancellationToken ct);
    Task SetActiveAsync(Guid userId, bool isActive, CancellationToken ct);

    // Signs the account out everywhere: every token issued before now stops being accepted.
    Task EndSessionsAsync(Guid userId, CancellationToken ct);

    Task<User> BuildLoginAsync(
        string firstName, string lastName, string email, string password, UserRole role, CancellationToken ct);

    Task ApplyLoginEditAsync(User user, string firstName, string lastName, string email, CancellationToken ct);
}
