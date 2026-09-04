using EduCore.Dtos.Pagination;
using EduCore.Dtos.Statistics;
using EduCore.Dtos.Statistics.Aggregate;
using EduCore.Dtos.Statistics.Individual;

namespace EduCore.Services.Statistics.Interfaces;

public interface IClassStatisticsService
{
    Task<IReadOnlyList<ClassSummaryDto>> GetSummariesAsync(StatisticsFilter filter, CancellationToken ct);

    // Same summaries as GetSummariesAsync, but only the requested page of classes is
    // aggregated in the database. Pages hold the same summary set as the full list.
    Task<PagedResultDto<ClassSummaryDto>> GetSummariesPagedAsync(
        StatisticsFilter filter, PaginationQueryDto pagination, CancellationToken ct);

    Task<ClassDetailStatisticsDto> GetDetailAsync(StatisticsFilter filter, Guid classId, CancellationToken ct);
}
