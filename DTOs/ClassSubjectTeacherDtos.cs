namespace EduCore.Dtos;

// The per-class, per-subject teaching assignment -- this is the DTO that actually
// answers "who teaches Class X's Math".
public record ClassSubjectTeacherListDto(
    Guid Id,
    Guid ClassId,
    string ClassName,
    Guid SubjectId,
    string SubjectName,
    Guid TeacherId,
    string TeacherName);

public record ClassSubjectTeacherCreateDto(Guid ClassId, Guid SubjectId, Guid TeacherId);

// Reassigning who teaches an existing class-subject pairing (e.g. a substitute
// or a permanent replacement) without touching the Class/Subject link itself.
public record ClassSubjectTeacherEditDto(Guid TeacherId);
