using EduCore.Dtos.Statistics;
using EduCore.Dtos.Statistics.Aggregate;
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

        var overall = await academic.GetTotalsAsync(ctx, ct);
        var schoolAverage = overall.AverageFinalMark is null
            ? null
            : (double?)Math.Round(overall.AverageFinalMark.Value, 2);

        var attendanceTotals = await attendance.GetTotalsAsync(ctx, ct);
        var attendanceBaseline = attendanceTotals.Records == 0
            ? (double?)null
            : Math.Round(attendanceTotals.Present * 100.0 / attendanceTotals.Records, 2);

        return studentsByClass
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
                    atRisk.GetValueOrDefault(sc.ClassId.ToString()));
            })
            .ToList();
    }
}
