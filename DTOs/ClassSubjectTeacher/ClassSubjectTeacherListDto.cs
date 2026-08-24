namespace EduCore.Dtos.ClassSubjectTeacher;

// The per-class, per-subject teaching assignment -- this is the DTO that actually
// answers "who teaches Class X's Math".
public record ClassSubjectTeacherListDto(
    Guid Id,
    Guid ClassId,
    string ClassName,
    Guid SubjectId,
    string SubjectName,
    Guid TeacherId,
    string TeacherName,
    DateOnly? EndedOn);
