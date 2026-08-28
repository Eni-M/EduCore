namespace EduCore.Dtos.Student;

// Whether this student may graduate right now, and if not, the sentence explaining why.
//
// Computed on the server and handed to the client whole, rather than letting the screen
// re-derive it. The screen cannot: the rule needs the class's grade, its academic year's
// terms, and every subject the class is taught, none of which a student's page loads. The
// front end previously kept its own copy of a simpler version of this rule, which is exactly
// the drift this removes -- the button and the endpoint now agree by construction.
public record GraduationEligibilityDto(bool CanGraduate, string? Reason);
