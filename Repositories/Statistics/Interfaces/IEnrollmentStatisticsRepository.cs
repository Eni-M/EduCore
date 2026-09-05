using EduCore.Dtos.Pagination;
using EduCore.Dtos.Statistics;
using EduCore.Dtos.Statistics.Rows;
using EduCore.Services.Statistics;

namespace EduCore.Repositories.Statistics.Interfaces;

public interface IEnrollmentStatisticsRepository
{
    Task<int> CountActiveAsync(StatisticsQuery ctx, CancellationToken ct);
    Task<int> CountStartedAsync(StatisticsQuery ctx, CancellationToken ct);
    Task<int> CountExitedAsync(StatisticsQuery ctx, CancellationToken ct);

    Task<int> CountActiveOnAsync(StatisticsQuery ctx, DateOnly on, CancellationToken ct);

    Task<IReadOnlyList<CountRow>> GetExitsByReasonAsync(StatisticsQuery ctx, CancellationToken ct);
    Task<IReadOnlyList<CountRow>> GetByAcademicYearAsync(StatisticsQuery ctx, CancellationToken ct);

    Task<IReadOnlyList<ClassCapacityRow>> GetCapacityAsync(StatisticsQuery ctx, CancellationToken ct);

    // Same classes as GetCapacityAsync, ordered totally and sliced in the database.
    Task<PagedResultDto<ClassCapacityRow>> GetCapacityPagedAsync(
        StatisticsQuery ctx, PaginationQueryDto pagination, CancellationToken ct);
}
