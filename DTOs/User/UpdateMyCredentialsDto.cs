namespace EduCore.Dtos.User;

// CurrentPassword is required only when Email changes; renaming yourself needs no re-entry.
public record UpdateMyCredentialsDto(string FirstName, string LastName, string Email, string? CurrentPassword = null);
