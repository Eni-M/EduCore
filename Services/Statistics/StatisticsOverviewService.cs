using EduCore.Repositories.Interfaces;
using EduCore.Dtos.Statistics;
using EduCore.Dtos.Statistics.Aggregate;
using EduCore.Repositories.Statistics.Interfaces;
using EduCore.Services.Statistics.Interfaces;

namespace EduCore.Services.Statistics;

// The two dashboard endpoints. These compose the focused services and define no metric of
// their own, so nothing here can drift from what the granular endpoints report. Neither
// returns a record that names an individual.
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

    // Supersedes the old GET /api/schools/statistics, which returned nine bare counts with no
    // period, no context and no per-school breakdown of outcomes.
    public async Task<PlatformOverviewDto> GetPlatformOverviewAsync(
        StatisticsFilter filter, CancellationToken ct)
    {
        // Resolved and discarded on purpose: it enforces the caller's access before the loop
        // below starts querying school by school.
        await scope.ResolveAsync(filter, ct);

        // Every school, including switched-off ones: a school that closed still has a year of
        // history, and dropping it here would silently change what the platform totals mean.
        var allSchools = (await schools.GetAllOrderedAsync(activeOnly: false, ct)).ToList();
        var bySchool = new List<SchoolComparisonDto>();

        // One pass per school. Fine for a network of this size, but it is the most expensive
        // request in the API: the per-school work is a handful of queries each, and it grows
        // linearly with the number of schools.
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
