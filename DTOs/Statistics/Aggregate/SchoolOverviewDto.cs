namespace EduCore.Dtos.Statistics.Aggregate;

public record SchoolOverviewDto(
    StudentStatisticsDto Students,
    EnrollmentStatisticsDto Enrollment,
    AttendanceStatisticsDto Attendance,
    AcademicStatisticsDto Academic,
    TeacherStatisticsDto Teachers,
    IReadOnlyList<ClassSummaryDto> Classes);
