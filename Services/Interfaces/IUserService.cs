using EduCore.Dtos.User;
using EduCore.Enums;
using EduCore.Models;

namespace EduCore.Services.Interfaces;

public interface IUserService
{
    Task<AuthResponseDto> LoginAsync(LoginUserDto dto, CancellationToken ct);

    Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto, CancellationToken ct);
    Task<ResetPasswordResponseDto> AdminResetPasswordAsync(Guid userId, CancellationToken ct);
    Task SetActiveAsync(Guid userId, bool isActive, CancellationToken ct);

    Task<User> BuildLoginAsync(
        string firstName, string lastName, string email, string password, UserRole role, CancellationToken ct);

    Task ApplyLoginEditAsync(User user, string firstName, string lastName, string email, CancellationToken ct);
}
