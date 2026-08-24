namespace EduCore.Dtos.DiaryEntry;

// Who taught it, which class/subject, and when are the entry's identity -- not editable
// after the fact. Only the lesson plan/notes can be revised.
public record DiaryEntryEditDto(string Topic, string? Notes);
