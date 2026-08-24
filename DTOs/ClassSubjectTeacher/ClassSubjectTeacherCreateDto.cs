namespace EduCore.Dtos.ClassSubjectTeacher;

public record ClassSubjectTeacherCreateDto(Guid ClassId, Guid SubjectId, Guid TeacherId);
