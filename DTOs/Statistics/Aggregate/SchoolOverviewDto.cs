namespace EduCore.Dtos.Statistics.Aggregate;

// A thin composition of the focused services for the Admin dashboard. It defines no metrics
// of its own, and holds no record that names an individual.
public record SchoolOverviewDto(
    StudentStatisticsDto Students,
    EnrollmentStatisticsDto Enrollment,
    AttendanceStatisticsDto Attendance,
    AcademicStatisticsDto Academic,
    TeacherStatisticsDto Teachers,
    IReadOnlyList<ClassSummaryDto> Classes);
