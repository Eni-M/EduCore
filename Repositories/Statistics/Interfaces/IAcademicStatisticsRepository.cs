using EduCore.Dtos.Statistics.Aggregate;
using EduCore.Dtos.Statistics.Individual;
using EduCore.Services.Statistics;

namespace EduCore.Repositories.Statistics.Interfaces;

public interface IAcademicStatisticsRepository
{
    Task<AcademicTotals> GetTotalsAsync(StatisticsContext ctx, CancellationToken ct);
    Task<IReadOnlyList<AcademicGroup>> GetBySubjectAsync(StatisticsContext ctx, CancellationToken ct);
    Task<IReadOnlyList<AcademicGroup>> GetByClassAsync(StatisticsContext ctx, CancellationToken ct);
    Task<IReadOnlyList<AcademicGroup>> GetByGradeAsync(StatisticsContext ctx, CancellationToken ct);
    Task<IReadOnlyList<GradeDistributionDto>> GetDistributionAsync(StatisticsContext ctx, CancellationToken ct);
    Task<IReadOnlyList<AtRiskStudentDto>> GetAtRiskAsync(StatisticsContext ctx, CancellationToken ct);

    // The school-wide average for a subject at a grade level -- the baseline every per-class
    // and per-teacher figure is reported against.
    Task<IReadOnlyList<AcademicGroup>> GetSubjectGradeBaselinesAsync(
        StatisticsContext ctx, CancellationToken ct);
}
