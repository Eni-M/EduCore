namespace EduCore.Dtos.Statistics.Aggregate;

public record SchoolOverviewDto(
    StudentStatisticsDto Students,
    EnrollmentStatisticsDto Enrollment,
    AttendanceStatisticsDto Attendance,
    AcademicStatisticsDto Academic,
    TeacherStatisticsDto Teachers,
    // The class list can be large (a whole-region view especially), so it is paged. The summary
    // figures above are always over the full data set; only this list is a page.
    IReadOnlyList<ClassSummaryDto> Classes,
    int ClassesPage,
    int ClassesPageSize,
    int ClassesTotalCount)
{
    public int ClassesTotalPages =>
        ClassesPageSize == 0 ? 0 : (int)Math.Ceiling(ClassesTotalCount / (double)ClassesPageSize);
}
