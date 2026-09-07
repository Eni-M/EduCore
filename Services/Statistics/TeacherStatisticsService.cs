using EduCore.Dtos.Pagination;
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

    public async Task<PagedResultDto<TeacherWorkloadDto>> GetWorkloadPagedAsync(
        StatisticsFilter filter, PaginationQueryDto pagination, CancellationToken ct) =>
        await teachers.GetWorkloadPagedAsync(await scope.ResolveAsync(filter, ct), pagination, ct);

    public async Task<PagedResultDto<TeacherCoverageDto>> GetCoveragePagedAsync(
        StatisticsFilter filter, PaginationQueryDto pagination, CancellationToken ct) =>
        await teachers.GetCoveragePagedAsync(await scope.ResolveAsync(filter, ct), pagination, ct);

    public async Task<TeacherDetailStatisticsDto> GetDetailAsync(
        StatisticsFilter filter, Guid teacherId, CancellationToken ct)
    {
        var ctx = await scope.ResolveAsync(filter, ct);

        _ = await teachers.GetTeacherNameAsync(ctx, teacherId, ct)
            ?? throw new NotFoundException("Teacher", teacherId);

        var scopedToTeacher = ctx with { TeacherId = teacherId };

        var workload = (await teachers.GetWorkloadAsync(scopedToTeacher, ct)).Single();
        var coverage = (await teachers.GetCoverageAsync(scopedToTeacher, ct)).Single();

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
