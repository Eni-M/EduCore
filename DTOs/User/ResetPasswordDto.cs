namespace EduCore.Dtos.User;

public record ResetPasswordDto(
    string Token,
    string NewPassword,
    string ConfirmNewPassword);
