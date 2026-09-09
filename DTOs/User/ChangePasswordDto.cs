namespace EduCore.Dtos.User;

// The challenge and code are required for every role, not only the ones that must use a second
// step to sign in: changing a password is the action that takes an account away from its owner,
// so it is confirmed out of band even when the session doing it is already trusted.
public record ChangePasswordDto(
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword,
    Guid ChallengeId,
    string Code);
