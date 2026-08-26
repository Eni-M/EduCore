namespace EduCore.Dtos.Statistics.Aggregate;

// SuperAdmin cross-school view. Supersedes the old /api/schools/statistics.
public record PlatformOverviewDto(
    int Schools,
    StudentStatisticsDto Students,
    AttendanceStatisticsDto Attendance,
    AcademicStatisticsDto Academic,
    TeacherStatisticsDto Teachers,
    IReadOnlyList<SchoolComparisonDto> BySchool);
