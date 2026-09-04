using EduCore.Dtos.Pagination;
using EduCore.Dtos.Statistics;
using EduCore.Dtos.Statistics.Aggregate;
using EduCore.Dtos.Statistics.Rows;
using EduCore.Repositories.Statistics.Interfaces;
using EduCore.Services.Statistics.Interfaces;

namespace EduCore.Services.Statistics;

public class ClassStatisticsService(
    IAcademicStatisticsRepository academic,
    IAttendanceStatisticsRepository attendance,
    IStudentStatisticsRepository students,
    IStatisticsScopeResolver scope,
    ICellSuppressor suppressor) : IClassStatisticsService
{
    public async Task<IReadOnlyList<ClassSummaryDto>> GetSummariesAsync(
        StatisticsFilter filter, CancellationToken ct)
    {
        var ctx = await scope.ResolveAsync(filter, ct);

        return await BuildSummariesAsync(ctx, ct);
    }

    public async Task<PagedResultDto<ClassSummaryDto>> GetSummariesPagedAsync(
        StatisticsFilter filter, PaginationQueryDto pagination, CancellationToken ct)
    {
        var ctx = await scope.ResolveAsync(filter, ct);

        return await BuildSummariesPagedAsync(ctx, pagination, ct);
    }

    public async Task<ClassDetailStatisticsDto> GetDetailAsync(
        StatisticsFilter filter, Guid classId, CancellationToken ct)
    {
        var ctx = await scope.ResolveAsync(filter with { ClassId = classId }, ct);

        var summary = (await BuildSummariesAsync(ctx, ct)).FirstOrDefault()
                      ?? new ClassSummaryDto(classId, "Unknown class", 0, 0,
                          Measure.None(), Measure.None(), Measure.None(), 0);

        var overall = await academic.GetTotalsAsync(ctx, ct);
        var schoolAverage = overall.AverageFinalMark is null
            ? null
            : (double?)Math.Round(overall.AverageFinalMark.Value, 2);

        var subjects = (await academic.GetBySubjectAsync(ctx, ct))
            .Select(s => new AcademicByKeyDto(
                s.Key,
                s.Label,
                s.GradedMarks,
                suppressor.Apply(Measures
                    .Average(s.AverageFinalMark, s.GradedMarks)
                    .WithBaseline(schoolAverage, "this class, all subjects")),
                suppressor.Apply(Measures.Rate(s.Passed, s.GradedMarks))))
            .ToList();

        var attendanceTotals = await attendance.GetTotalsAsync(ctx, ct);
        var attendanceBaseline = attendanceTotals.Records == 0
            ? (double?)null
            : Math.Round(attendanceTotals.Present * 100.0 / attendanceTotals.Records, 2);

        var attendanceBySubject = (await attendance.GetBySubjectAsync(ctx, ct))
            .Select(a => new AttendanceByKeyDto(
                a.Key,
                a.Label,
                a.Records,
                suppressor.Apply(Measures
                    .Rate(a.Present, a.Records)
                    .WithBaseline(attendanceBaseline, "this class, all subjects")),
                suppressor.Apply(Measures.Rate(a.ExcusedAbsences, a.Records))))
            .ToList();

        return new ClassDetailStatisticsDto(
            summary,
            subjects,
            attendanceBySubject,
            SuppressDistribution(await academic.GetDistributionAsync(ctx, ct)));
    }

    private GradeDistributionResultDto SuppressDistribution(GradeDistributionResultDto distribution) =>
        suppressor.ShouldSuppress(distribution.TotalMarks)
            ? distribution with { Suppressed = true, Buckets = [] }
            : distribution;

    private async Task<IReadOnlyList<ClassSummaryDto>> BuildSummariesAsync(
        StatisticsQuery ctx, CancellationToken ct)
    {
        var academicByClass = (await academic.GetByClassAsync(ctx, ct)).ToDictionary(a => a.Key);
        var attendanceByClass = (await attendance.GetByClassAsync(ctx, ct)).ToDictionary(a => a.Key);
        var studentsByClass = await students.GetByClassAsync(ctx, ct);

        var atRisk = (await academic.GetAtRiskAsync(ctx, ct))
            .GroupBy(s => s.ClassId.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var (schoolAverage, attendanceBaseline) = await BaselinesAsync(ctx, ct);

        return ComposeSummaries(
            studentsByClass, academicByClass, attendanceByClass, atRisk,
            schoolAverage, attendanceBaseline);
    }

    private async Task<PagedResultDto<ClassSummaryDto>> BuildSummariesPagedAsync(
        StatisticsQuery ctx, PaginationQueryDto pagination, CancellationToken ct)
    {
        // The page of classes is fixed first (ORDER BY + Skip/Take in the database);
        // every aggregate below is then restricted to those class ids, so the database
        // never calculates summaries for classes the caller will not receive.
        var classPage = await students.GetByClassPagedAsync(ctx, pagination, ct);
        var classIds = classPage.Items.Select(r => r.ClassId).ToList();

        // An out-of-range page has no classes: skip the per-class aggregates entirely.
        // The scope-level baselines below still run so headline figures stay full-scope.
        var academicByClass = classIds.Count == 0
            ? new Dictionary<string, AcademicGroup>()
            : (await academic.GetByClassAsync(ctx, classIds, ct)).ToDictionary(a => a.Key);
        var attendanceByClass = classIds.Count == 0
            ? new Dictionary<string, AttendanceGroup>()
            : (await attendance.GetByClassAsync(ctx, classIds, ct)).ToDictionary(a => a.Key);
        var atRisk = classIds.Count == 0
            ? new Dictionary<string, int>()
            : (await academic.GetAtRiskCountsByClassAsync(ctx, classIds, ct))
                .ToDictionary(r => r.ClassId.ToString(), r => r.Count);

        var (schoolAverage, attendanceBaseline) = await BaselinesAsync(ctx, ct);

        return new PagedResultDto<ClassSummaryDto>(
            ComposeSummaries(
                classPage.Items, academicByClass, attendanceByClass, atRisk,
                schoolAverage, attendanceBaseline),
            classPage.Page,
            classPage.PageSize,
            classPage.TotalCount);
    }

    private async Task<(double? SchoolAverage, double? AttendanceBaseline)> BaselinesAsync(
        StatisticsQuery ctx, CancellationToken ct)
    {
        var overall = await academic.GetTotalsAsync(ctx, ct);
        var schoolAverage = overall.AverageFinalMark is null
            ? null
            : (double?)Math.Round(overall.AverageFinalMark.Value, 2);

        var attendanceTotals = await attendance.GetTotalsAsync(ctx, ct);
        var attendanceBaseline = attendanceTotals.Records == 0
            ? (double?)null
            : Math.Round(attendanceTotals.Present * 100.0 / attendanceTotals.Records, 2);

        return (schoolAverage, attendanceBaseline);
    }

    private IReadOnlyList<ClassSummaryDto> ComposeSummaries(
        IEnumerable<ClassCountRow> studentsByClass,
        IReadOnlyDictionary<string, AcademicGroup> academicByClass,
        IReadOnlyDictionary<string, AttendanceGroup> attendanceByClass,
        IReadOnlyDictionary<string, int> atRiskByClass,
        double? schoolAverage,
        double? attendanceBaseline) =>
        studentsByClass
            .Select(sc =>
            {
                academicByClass.TryGetValue(sc.ClassId.ToString(), out var a);
                attendanceByClass.TryGetValue(sc.ClassId.ToString(), out var att);

                return new ClassSummaryDto(
                    sc.ClassId,
                    sc.ClassName,
                    sc.Grade,
                    sc.Count,
                    att is null
                        ? Measure.None()
                        : suppressor.Apply(Measures
                            .Rate(att.Present, att.Records)
                            .WithBaseline(attendanceBaseline, "school attendance rate")),
                    a is null
                        ? Measure.None()
                        : suppressor.Apply(Measures
                            .Average(a.AverageFinalMark, a.GradedMarks)
                            .WithBaseline(schoolAverage, "school average")),
                    a is null
                        ? Measure.None()
                        : suppressor.Apply(Measures.Rate(a.Passed, a.GradedMarks)),
                    atRiskByClass.GetValueOrDefault(sc.ClassId.ToString()));
            })
            .ToList();
}
