using EduCore.Dtos.Attendance;

namespace EduCore.Dtos.DiaryEntry;

public record DiaryEntryDetailDto(
    Guid Id,
    Guid ClassId,
    string ClassName,
    Guid SubjectId,
    string SubjectName,
    Guid TeacherId,
    string TeacherName,
    DateOnly Date,
    int Period,
    string Topic,
    string? Notes,
    IReadOnlyList<AttendanceListDto> Attendances);
