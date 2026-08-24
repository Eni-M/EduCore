namespace EduCore.Dtos;

public record MarkListDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    Guid SubjectId,
    string SubjectName,
    Guid TeacherId,
    string TeacherName,
    int Value,
    DateOnly Date);

// TeacherId is passed explicitly for now; once auth/user-context is wired into the
// API layer this should come from the logged-in teacher instead of client input.
public record MarkCreateDto(Guid StudentId, Guid SubjectId, Guid TeacherId, int Value, DateOnly Date);
