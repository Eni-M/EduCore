namespace EduCore.Dtos.DiaryEntry;

public record DiaryEntryListDto(
    Guid Id,
    Guid ClassId,
    string ClassName,
    Guid SubjectId,
    string SubjectName,
    Guid TeacherId,
    string TeacherName,
    DateOnly Date,
    int Period,
    string Topic);
