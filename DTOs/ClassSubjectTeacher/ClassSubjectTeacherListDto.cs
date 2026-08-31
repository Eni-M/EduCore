namespace EduCore.Dtos.ClassSubjectTeacher;

public record ClassSubjectTeacherListDto(
    Guid Id,
    Guid ClassId,
    string ClassName,
    Guid SubjectId,
    string SubjectName,
    Guid TeacherId,
    string TeacherName,
    DateOnly? EndedOn);
