using EduCore.Dtos.Statistics;
using EduCore.Dtos.Statistics.Rows;
using EduCore.Dtos.Statistics.Aggregate;
using EduCore.Dtos.Statistics.Individual;
using EduCore.Services.Statistics;

namespace EduCore.Repositories.Statistics.Interfaces;

public interface IAcademicStatisticsRepository
{
    Task<AcademicTotals> GetTotalsAsync(StatisticsQuery ctx, CancellationToken ct);
    Task<IReadOnlyList<AcademicGroup>> GetBySubjectAsync(StatisticsQuery ctx, CancellationToken ct);
    Task<IReadOnlyList<AcademicGroup>> GetByClassAsync(StatisticsQuery ctx, CancellationToken ct);
    Task<IReadOnlyList<AcademicGroup>> GetByGradeAsync(StatisticsQuery ctx, CancellationToken ct);
    Task<GradeDistributionResultDto> GetDistributionAsync(StatisticsQuery ctx, CancellationToken ct);
    Task<IReadOnlyList<AtRiskStudentDto>> GetAtRiskAsync(StatisticsQuery ctx, CancellationToken ct);

    // The school-wide average for a subject at a grade level -- the baseline every per-class
    // and per-teacher figure is reported against.
    Task<IReadOnlyList<AcademicGroup>> GetSubjectGradeBaselinesAsync(
        StatisticsQuery ctx, CancellationToken ct);
}
