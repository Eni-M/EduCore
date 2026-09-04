namespace EduCore.Dtos.Curriculum;

public record GradeCurriculumEntryDto(Guid Id, int Grade, Guid SubjectId, string SubjectName);
