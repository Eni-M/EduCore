using EduCore.Dtos.Statistics;
using EduCore.Dtos.Statistics.Aggregate;
using EduCore.Dtos.Statistics.Individual;
using EduCore.Exceptions;
using EduCore.Repositories.Statistics;
using EduCore.Repositories.Statistics.Interfaces;
using EduCore.Services.Statistics.Interfaces;

namespace EduCore.Services.Statistics;

public class TeacherStatisticsService(
    ITeacherStatisticsRepository teachers,
    IAcademicStatisticsRepository academic,
    IStudentStatisticsRepository students,
    IStatisticsScopeResolver scope,
    ICellSuppressor suppressor) : ITeacherStatisticsService
{
    public async Task<TeacherStatisticsDto> GetSummaryAsync(
        StatisticsFilter filter, CancellationToken ct)
    {
        var ctx = await scope.ResolveAsync(filter, ct);

        var teacherCount = await teachers.CountTeachersAsync(ctx, ct);
        var studentTotals = await students.GetTotalsAsync(ctx, ct);
        var workload = await teachers.GetWorkloadAsync(ctx, ct);

        // Suppressed like any other aggregate: at a school with a handful of teachers -- or
        // with a teacherId filter applied -- these describe one identifiable person.
        return new TeacherStatisticsDto(
            teacherCount,
            teacherCount == 0
                ? Measure.None()
                : suppressor.Apply(
                    Measures.Average((double)studentTotals.Active / teacherCount, teacherCount)),
            workload.Count == 0
                ? Measure.None()
                : suppressor.Apply(
                    Measures.Average(workload.Average(w => (double)w.Classes), workload.Count)));
    }

    public async Task<IReadOnlyList<TeacherWorkloadDto>> GetWorkloadAsync(
        StatisticsFilter filter, CancellationToken ct) =>
        await teachers.GetWorkloadAsync(await scope.ResolveAsync(filter, ct), ct);

    public async Task<IReadOnlyList<TeacherCoverageDto>> GetCoverageAsync(
        StatisticsFilter filter, CancellationToken ct) =>
        await teachers.GetCoverageAsync(await scope.ResolveAsync(filter, ct), ct);

    // The teacher view. Work metrics and outcome metrics are kept apart deliberately, and no
    // outcome number is ever returned bare -- see TeacherDetailStatisticsDto for why.
    public async Task<TeacherDetailStatisticsDto> GetDetailAsync(
        StatisticsFilter filter, Guid teacherId, CancellationToken ct)
    {
        var ctx = await scope.ResolveAsync(filter, ct);

        // Confirms the teacher exists and is inside the caller's school before anything else
        // is queried -- GetTeacherNameAsync is scoped, so a foreign teacher reads as missing.
        _ = await teachers.GetTeacherNameAsync(ctx, teacherId, ct)
            ?? throw new NotFoundException("Teacher", teacherId);

        var scopedToTeacher = ctx with { TeacherId = teacherId };

        var workload = (await teachers.GetWorkloadAsync(scopedToTeacher, ct)).Single();
        var coverage = (await teachers.GetCoverageAsync(scopedToTeacher, ct)).Single();

        // Baselines come from the whole school, not from this teacher's own classes -- that is
        // the entire point of showing them.
        var baselines = (await academic.GetSubjectGradeBaselinesAsync(ctx, ct))
            .ToDictionary(b => b.Key, b => b);

        var performance = (await teachers.GetPerformanceAsync(ctx, teacherId, ct))
            .Select(p =>
            {
                var key = AcademicStatisticsRepository.BaselineKey(p.SubjectId, p.Grade);
                baselines.TryGetValue(key, out var baseline);

                return new TeacherSubjectPerformanceDto(
                    p.ClassId,
                    p.ClassName,
                    p.Grade,
                    p.SubjectId,
                    p.SubjectName,
                    p.GradedMarks,
                    suppressor.Apply(Measures
                        .Average(p.Average, p.GradedMarks)
                        .WithBaseline(
                            baseline?.AverageFinalMark is null
                                ? null
                                : Math.Round(baseline.AverageFinalMark.Value, 2),
                            $"school average, {p.SubjectName}, grade {p.Grade}")),
                    suppressor.Apply(Measures.Rate(p.Passed, p.GradedMarks)));
            })
            .ToList();

        return new TeacherDetailStatisticsDto(workload, coverage, performance);
    }
}
