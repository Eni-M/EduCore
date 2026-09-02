namespace EduCore.Dtos.SchoolingHistory;

public record SchoolingHistoryEditDto(
    string PreviousSchoolName,
    string? City,
    DateOnly FromDate,
    DateOnly? ToDate,
    string? GradeOrLevel,
    string? ExitReason,
    string? Notes);
