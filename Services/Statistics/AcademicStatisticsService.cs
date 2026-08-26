using EduCore.Dtos.Statistics;
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

    public async Task<IReadOnlyList<AcademicByKeyDto>> GetByGradeAsync(
        StatisticsFilter filter, CancellationToken ct)
    {
        var ctx = await scope.ResolveAsync(filter, ct);

        return WithOverallBaseline(
            await academic.GetByGradeAsync(ctx, ct),
            await academic.GetTotalsAsync(ctx, ct),
            "school average across all grades");
    }

    public async Task<IReadOnlyList<GradeDistributionDto>> GetDistributionAsync(
        StatisticsFilter filter, CancellationToken ct) =>
        await academic.GetDistributionAsync(await scope.ResolveAsync(filter, ct), ct);

    // Individual-level: names students, so it is never suppressed and never public.
    public async Task<IReadOnlyList<AtRiskStudentDto>> GetAtRiskAsync(
        StatisticsFilter filter, CancellationToken ct) =>
        await academic.GetAtRiskAsync(await scope.ResolveAsync(filter, ct), ct);

    // "Class A vs Class B, Mathematics, 4.1 vs 3.6". Each class is reported against the
    // school-wide average for that subject, so the comparison is between the classes AND
    // against the school, not just between two bare numbers.
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
