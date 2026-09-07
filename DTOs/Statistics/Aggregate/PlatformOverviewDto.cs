namespace EduCore.Dtos.Statistics.Aggregate;

public record PlatformOverviewDto(
    int Schools,
    StudentStatisticsDto Students,
    AttendanceStatisticsDto Attendance,
    AcademicStatisticsDto Academic,
    TeacherStatisticsDto Teachers,
    // One row per school nationwide, so this is paged. The summary figures above are always
    // over every school in scope; only this list is a page.
    IReadOnlyList<SchoolComparisonDto> BySchool,
    int BySchoolPage,
    int BySchoolPageSize,
    int BySchoolTotalCount)
{
    public int BySchoolTotalPages =>
        BySchoolPageSize == 0 ? 0 : (int)Math.Ceiling(BySchoolTotalCount / (double)BySchoolPageSize);
}
