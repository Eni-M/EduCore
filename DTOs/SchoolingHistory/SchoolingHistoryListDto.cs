namespace EduCore.Dtos.SchoolingHistory;

public record SchoolingHistoryListDto(
    Guid Id,
    Guid StudentId,
    string PreviousSchoolName,
    string? City,
    DateOnly FromDate,
    DateOnly? ToDate,
    string? GradeOrLevel,
    string? ExitReason,
    string? Notes);
