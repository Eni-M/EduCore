using EduCore.Dtos.Mark;

namespace EduCore.Dtos.Student;

public record StudentSummaryDto(
    Guid? CurrentClassId,
    string? CurrentClassName,
    IReadOnlyList<MarkListDto> Marks,
    int AttendanceRecorded,
    int AttendancePresent);
