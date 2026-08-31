using EduCore.Repositories.Interfaces;
using EduCore.Dtos.Statistics;
using EduCore.Dtos.Statistics.Aggregate;
using EduCore.Repositories.Statistics.Interfaces;
using EduCore.Services.Statistics.Interfaces;

namespace EduCore.Services.Statistics;

public class StatisticsOverviewService(
    IStudentStatisticsService students,
    IEnrollmentStatisticsService enrollment,
    IAttendanceStatisticsService attendance,
    IAcademicStatisticsService academic,
    ITeacherStatisticsService teachers,
    IClassStatisticsService classes,
    IStatisticsScopeResolver scope,
    ISchoolRepository schools) : IStatisticsOverviewService
{
    public async Task<SchoolOverviewDto> GetSchoolOverviewAsync(
        StatisticsFilter filter, CancellationToken ct) =>
        new(
            await students.GetSummaryAsync(filter, ct),
            await enrollment.GetSummaryAsync(filter, ct),
            await attendance.GetSummaryAsync(filter, ct),
            await academic.GetSummaryAsync(filter, ct),
            await teachers.GetSummaryAsync(filter, ct),
            await classes.GetSummariesAsync(filter, ct));

    public async Task<PlatformOverviewDto> GetPlatformOverviewAsync(
        StatisticsFilter filter, CancellationToken ct)
    {
        await scope.ResolveAsync(filter, ct);

        var allSchools = (await schools.GetAllOrderedAsync(activeOnly: false, ct)).ToList();
        var bySchool = new List<SchoolComparisonDto>();

        foreach (var school in allSchools)
        {
            var scoped = filter with { SchoolId = school.Id };

            var schoolStudents = await students.GetSummaryAsync(scoped, ct);
            var schoolTeachers = await teachers.GetSummaryAsync(scoped, ct);
            var schoolAttendance = await attendance.GetSummaryAsync(scoped, ct);
            var schoolAcademic = await academic.GetSummaryAsync(scoped, ct);
            var schoolClasses = await classes.GetSummariesAsync(scoped, ct);

            bySchool.Add(new SchoolComparisonDto(
                school.Id,
                school.Name,
                school.Type,
                schoolStudents.Active,
                schoolTeachers.Teachers,
                schoolClasses.Count,
                schoolAttendance.AttendanceRate,
                schoolAcademic.AverageFinalMark,
                schoolAcademic.PassRate));
        }

        return new PlatformOverviewDto(
            allSchools.Count,
            await students.GetSummaryAsync(filter, ct),
            await attendance.GetSummaryAsync(filter, ct),
            await academic.GetSummaryAsync(filter, ct),
            await teachers.GetSummaryAsync(filter, ct),
            bySchool);
    }
}
