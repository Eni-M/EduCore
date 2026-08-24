using EduCore.Dtos.User;

namespace EduCore.Services.Interfaces;

public interface IUserService
{
    Task<AuthResponseDto> LoginAsync(LoginUserDto dto, CancellationToken ct);

    Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto, CancellationToken ct);
    Task<ResetPasswordResponseDto> AdminResetPasswordAsync(Guid userId, CancellationToken ct);
}
