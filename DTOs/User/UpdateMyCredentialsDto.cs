namespace EduCore.Dtos.User;

// Every signed-in user's own name and email, changed by themselves rather than
// by whoever administers their account. Distinct from the per-role edit DTOs
// (StudentEditDto and the rest) because those also carry fields -- a student's
// city, a class's grade -- that are none of the person's own business to write,
// and are reached through an Admin-only route for exactly that reason.
public record UpdateMyCredentialsDto(string FirstName, string LastName, string Email);
