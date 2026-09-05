using EduCore.Dtos.User;

namespace EduCore.Services.Interfaces;

public interface IPasswordResetService
{
    // Issues a reset link if the address belongs to an active account. Returns nothing either
    // way: the endpoint must not reveal whether an account exists.
    Task RequestAsync(ForgotPasswordDto dto, CancellationToken ct);

    // Redeems a token and sets the new password. Throws if the token is unknown, expired,
    // already used, or the account has since been deactivated.
    Task ResetAsync(ResetPasswordDto dto, CancellationToken ct);
}
