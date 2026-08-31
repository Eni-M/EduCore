namespace EduCore.Dtos.DiaryEntry;

public record DiaryEntryCreateDto(
    Guid ClassId,
    Guid SubjectId,
    Guid? TeacherId,
    DateOnly Date,
    int Period,
    string Topic,
    string? Notes);
