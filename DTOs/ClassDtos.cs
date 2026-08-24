namespace EduCore.Dtos;

public record ClassListDto(
    Guid Id,
    string Name,
    int Grade,
    Guid SchoolId,
    string SchoolName,
    Guid? HomeroomTeacherId,
    string? HomeroomTeacherName);

public record ClassCreateDto(string Name, Guid SchoolId, int Grade);

// SchoolId is intentionally not editable here -- moving a class to another school
// isn't a plain field edit, it's a separate operation if it's ever needed at all.
public record ClassEditDto(string Name, int Grade);

// Matches the dedicated Class.AssignHomeroomTeacher domain method rather than a generic edit.
public record ClassAssignHomeroomTeacherDto(Guid TeacherId);
