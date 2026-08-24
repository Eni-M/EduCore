namespace EduCore.Dtos.ClassSubjectTeacher;

// Ends an assignment (admin-supplied date, not clock-driven) without deleting it, so every
// DiaryEntry/Mark that referenced it stays valid, while freeing the class-subject pairing up
// for a new active assignment.
public record ClassSubjectTeacherEndDto(DateOnly EndedOn);
