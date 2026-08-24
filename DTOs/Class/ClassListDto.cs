namespace EduCore.Dtos.Class;

public record ClassListDto(
    Guid Id,
    string Name,
    int Grade,
    Guid SchoolId,
    string SchoolName,
    Guid AcademicYearId,
    string AcademicYearName,
    Guid? HomeroomTeacherId,
    string? HomeroomTeacherName);
