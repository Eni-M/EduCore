using EduCore.Enums;

namespace EduCore.Dtos.Statistics.Aggregate;

// One school's headline figures, for SuperAdmin cross-school comparison.
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
