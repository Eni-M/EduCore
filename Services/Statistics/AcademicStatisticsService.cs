using EduCore.Dtos.Pagination;
using EduCore.Dtos.Statistics;
using EduCore.Dtos.Statistics.Rows;
using EduCore.Dtos.Statistics.Aggregate;
using EduCore.Dtos.Statistics.Individual;
using EduCore.Repositories.Statistics;
using EduCore.Repositories.Statistics.Interfaces;
using EduCore.Services.Statistics.Interfaces;

namespace EduCore.Services.Statistics;

public class AcademicStatisticsService(
    IAcademicStatisticsRepository academic,
    IStatisticsScopeResolver scope,
    ICellSuppressor suppressor) : IAcademicStatisticsService
{
    public async Task<AcademicStatisticsDto> GetSummaryAsync(
        StatisticsFilter filter, CancellationToken ct)
    {
        var ctx = await scope.ResolveAsync(filter, ct);
        var totals = await academic.GetTotalsAsync(ctx, ct);

        return new AcademicStatisticsDto(
            totals.GradedMarks,
            totals.IncompleteMarks,
            suppressor.Apply(Measures.Average(totals.AverageFinalMark, totals.GradedMarks)),
            suppressor.Apply(Measures.Rate(totals.Passed, totals.GradedMarks)),
            suppressor.Apply(Measures.Rate(totals.Failed, totals.GradedMarks)),
            suppressor.Apply(Measures.Rate(totals.AtRisk, totals.GradedMarks)));
    }

    public async Task<IReadOnlyList<AcademicByKeyDto>> GetBySubjectAsync(
        StatisticsFilter filter, CancellationToken ct)
    {
        var ctx = await scope.ResolveAsync(filter, ct);

        return WithOverallBaseline(
            await academic.GetBySubjectAsync(ctx, ct),
            await academic.GetTotalsAsync(ctx, ct),
            "school average across all subjects");
    }

    public async Task<IReadOnlyList<AcademicByKeyDto>> GetByClassAsync(
        StatisticsFilter filter, CancellationToken ct)
    {
        var ctx = await scope.ResolveAsync(filter, ct);

        return WithOverallBaseline(
            await academic.GetByClassAsync(ctx, ct),
            await academic.GetTotalsAsync(ctx, ct),
            "school average across all classes");
    }

    public async Task<PagedResultDto<AcademicByKeyDto>> GetBySubjectPagedAsync(
        StatisticsFilter filter, PaginationQueryDto pagination, CancellationToken ct)
    {
        var ctx = await scope.ResolveAsync(filter, ct);
        var page = await academic.GetBySubjectPagedAsync(ctx, pagination, ct);

        // The baseline is the whole scope, not the page: a subject is compared against the
        // school average, which must not change as the reader turns the page.
        return Repage(page, WithOverallBaseline(
            page.Items, await academic.GetTotalsAsync(ctx, ct), "school average across all subjects"));
    }

    public async Task<PagedResultDto<AcademicByKeyDto>> GetByClassPagedAsync(
        StatisticsFilter filter, PaginationQueryDto pagination, CancellationToken ct)
    {
        var ctx = await scope.ResolveAsync(filter, ct);
        var page = await academic.GetByClassPagedAsync(ctx, pagination, ct);

        return Repage(page, WithOverallBaseline(
            page.Items, await academic.GetTotalsAsync(ctx, ct), "school average across all classes"));
    }

    private static PagedResultDto<AcademicByKeyDto> Repage(
        PagedResultDto<AcademicGroup> source, IReadOnlyList<AcademicByKeyDto> items) =>
        new(items, source.Page, source.PageSize, source.TotalCount);

    public async Task<IReadOnlyList<AcademicByKeyDto>> GetByGradeAsync(
        StatisticsFilter filter, CancellationToken ct)
    {
        var ctx = await scope.ResolveAsync(filter, ct);

        return WithOverallBaseline(
            await academic.GetByGradeAsync(ctx, ct),
            await academic.GetTotalsAsync(ctx, ct),
            "school average across all grades");
    }

    public async Task<GradeDistributionResultDto> GetDistributionAsync(
        StatisticsFilter filter, CancellationToken ct) =>
        Suppress(await academic.GetDistributionAsync(await scope.ResolveAsync(filter, ct), ct));

    private GradeDistributionResultDto Suppress(GradeDistributionResultDto distribution) =>
        suppressor.ShouldSuppress(distribution.TotalMarks)
            ? distribution with { Suppressed = true, Buckets = [] }
            : distribution;

    public async Task<IReadOnlyList<AtRiskStudentDto>> GetAtRiskAsync(
        StatisticsFilter filter, CancellationToken ct) =>
        await academic.GetAtRiskAsync(await scope.ResolveAsync(filter, ct), ct);

    public async Task<PagedResultDto<AtRiskStudentDto>> GetAtRiskPagedAsync(
        StatisticsFilter filter, PaginationQueryDto pagination, CancellationToken ct) =>
        await academic.GetAtRiskPagedAsync(await scope.ResolveAsync(filter, ct), pagination, ct);

    public async Task<SubjectComparisonDto> GetSubjectComparisonAsync(
        StatisticsFilter filter, Guid subjectId, CancellationToken ct)
    {
        var ctx = await scope.ResolveAsync(filter with { SubjectId = subjectId }, ct);

        var byClass = await academic.GetByClassAsync(ctx, ct);
        var subjects = await academic.GetBySubjectAsync(ctx, ct);
        var subject = subjects.FirstOrDefault(s => s.Key == subjectId.ToString());

        var rows = byClass
            .Select(c => new AcademicByKeyDto(
                c.Key,
                c.Label,
                c.GradedMarks,
                suppressor.Apply(Measures
                    .Average(c.AverageFinalMark, c.GradedMarks)
                    .WithBaseline(RoundOrNull(subject?.AverageFinalMark), "school average for this subject")),
                suppressor.Apply(Measures.Rate(c.Passed, c.GradedMarks))))
            .ToList();

        return new SubjectComparisonDto(subjectId, subject?.Label ?? "Unknown subject", rows);
    }

    private IReadOnlyList<AcademicByKeyDto> WithOverallBaseline(
        IEnumerable<AcademicGroup> groups, AcademicTotals overall, string baselineLabel)
    {
        var baseline = RoundOrNull(overall.AverageFinalMark);

        return groups
            .Select(g => new AcademicByKeyDto(
                g.Key,
                g.Label,
                g.GradedMarks,
                suppressor.Apply(Measures
                    .Average(g.AverageFinalMark, g.GradedMarks)
                    .WithBaseline(baseline, baselineLabel)),
                suppressor.Apply(Measures.Rate(g.Passed, g.GradedMarks))))
            .ToList();
    }

    private static double? RoundOrNull(double? value) =>
        value is null ? null : Math.Round(value.Value, 2);
}
