using EduCore.Enums;

namespace EduCore.Dtos.Statistics.Aggregate;

public record SchoolComparisonDto(
    Guid SchoolId,
    string SchoolName,
    SchoolType Type,
    int Students,
    int Teachers,
    int Classes,
    Measure AttendanceRate,
    Measure AverageFinalMark,
    Measure PassRate);
