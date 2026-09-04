using EduCore.Dtos.Pagination;
using EduCore.Dtos.Statistics;
using EduCore.Dtos.Statistics.Aggregate;
using EduCore.Dtos.Statistics.Individual;

namespace EduCore.Services.Statistics.Interfaces;

public interface IAcademicStatisticsService
{
    Task<AcademicStatisticsDto> GetSummaryAsync(StatisticsFilter filter, CancellationToken ct);
    Task<IReadOnlyList<AcademicByKeyDto>> GetBySubjectAsync(StatisticsFilter filter, CancellationToken ct);
    Task<IReadOnlyList<AcademicByKeyDto>> GetByClassAsync(StatisticsFilter filter, CancellationToken ct);
    Task<IReadOnlyList<AcademicByKeyDto>> GetByGradeAsync(StatisticsFilter filter, CancellationToken ct);
    Task<GradeDistributionResultDto> GetDistributionAsync(StatisticsFilter filter, CancellationToken ct);
    Task<IReadOnlyList<AtRiskStudentDto>> GetAtRiskAsync(StatisticsFilter filter, CancellationToken ct);

    // Same students as GetAtRiskAsync, aggregated per page in the database.
    Task<PagedResultDto<AtRiskStudentDto>> GetAtRiskPagedAsync(
        StatisticsFilter filter, PaginationQueryDto pagination, CancellationToken ct);
    Task<SubjectComparisonDto> GetSubjectComparisonAsync(
        StatisticsFilter filter, Guid subjectId, CancellationToken ct);
}
