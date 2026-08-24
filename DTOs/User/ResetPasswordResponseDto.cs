namespace EduCore.Dtos.User;

// The generated password is shown to the resetting admin once and never persisted in the database. The user will be required to change their password on next login.
public record ResetPasswordResponseDto(string Message, string TemporaryPassword);
