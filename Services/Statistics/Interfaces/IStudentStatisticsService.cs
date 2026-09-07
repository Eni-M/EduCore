using EduCore.Dtos.Pagination;
using EduCore.Dtos.Statistics;
using EduCore.Dtos.Statistics.Aggregate;
using EduCore.Dtos.Statistics.Individual;

namespace EduCore.Services.Statistics.Interfaces;

public interface IStudentStatisticsService
{
    Task<StudentStatisticsDto> GetSummaryAsync(StatisticsFilter filter, CancellationToken ct);
    Task<IReadOnlyList<CountByKeyDto>> GetByGradeAsync(StatisticsFilter filter, CancellationToken ct);
    Task<IReadOnlyList<CountByKeyDto>> GetByClassAsync(StatisticsFilter filter, CancellationToken ct);

    // Same classes as GetByClassAsync, grouped and sliced in the database.
    Task<PagedResultDto<CountByKeyDto>> GetByClassPagedAsync(
        StatisticsFilter filter, PaginationQueryDto pagination, CancellationToken ct);

    Task<IReadOnlyList<CountByKeyDto>> GetByAgeAsync(StatisticsFilter filter, CancellationToken ct);
    Task<IReadOnlyList<TrendPointDto>> GetEnrollmentTrendAsync(StatisticsFilter filter, CancellationToken ct);
}
