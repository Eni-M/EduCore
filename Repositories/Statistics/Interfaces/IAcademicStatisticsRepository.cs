using EduCore.Dtos.Pagination;
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

    // Same aggregation as GetByClassAsync, restricted to the requested classes so a
    // paged caller only pays for the page it returns.
    Task<IReadOnlyList<AcademicGroup>> GetByClassAsync(
        StatisticsQuery ctx, IReadOnlyCollection<Guid> classIds, CancellationToken ct);

    Task<IReadOnlyList<AcademicGroup>> GetByGradeAsync(StatisticsQuery ctx, CancellationToken ct);
    Task<GradeDistributionResultDto> GetDistributionAsync(StatisticsQuery ctx, CancellationToken ct);
    Task<IReadOnlyList<AtRiskStudentDto>> GetAtRiskAsync(StatisticsQuery ctx, CancellationToken ct);

    // Same rows as GetAtRiskAsync, ordered by average with a deterministic tie-break,
    // aggregated per page in the database: concatenated pages hold the same students.
    Task<PagedResultDto<AtRiskStudentDto>> GetAtRiskPagedAsync(
        StatisticsQuery ctx, PaginationQueryDto pagination, CancellationToken ct);

    // Same student-level risk rule as GetAtRiskAsync, but grouped to per-class counts in
    // the database. The class summary only needs the count, never the per-student rows.
    Task<IReadOnlyList<AtRiskClassCount>> GetAtRiskCountsByClassAsync(
        StatisticsQuery ctx, IReadOnlyCollection<Guid> classIds, CancellationToken ct);

    Task<IReadOnlyList<AcademicGroup>> GetSubjectGradeBaselinesAsync(
        StatisticsQuery ctx, CancellationToken ct);
}
