namespace EduCore.Dtos.Statistics.Aggregate;

public record PlatformOverviewDto(
    int Schools,
    StudentStatisticsDto Students,
    AttendanceStatisticsDto Attendance,
    AcademicStatisticsDto Academic,
    TeacherStatisticsDto Teachers,
    IReadOnlyList<SchoolComparisonDto> BySchool);
