namespace EduCore.Dtos.SchoolingHistory;

// StudentId comes from the route and SchoolId from the caller's own school, so neither is on the
// body -- a client cannot file a history row against a student in another school.
public record SchoolingHistoryCreateDto(
    string PreviousSchoolName,
    string? City,
    DateOnly FromDate,
    DateOnly? ToDate,
    string? GradeOrLevel,
    string? ExitReason,
    string? Notes);
