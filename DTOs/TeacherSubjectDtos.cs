namespace EduCore.Dtos;

// A teacher's subject qualifications -- shown as part of a Teacher's detail view.
public record TeacherSubjectListDto(Guid Id, Guid SubjectId, string SubjectName);

public record TeacherSubjectCreateDto(Guid TeacherId, Guid SubjectId);
