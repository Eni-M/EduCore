using EduCore.Dtos.Pagination;
using EduCore.Dtos.Statistics;
using EduCore.Dtos.Statistics.Rows;
using EduCore.Dtos.Statistics.Aggregate;
using EduCore.Services.Statistics;

namespace EduCore.Repositories.Statistics.Interfaces;

public interface IStudentStatisticsRepository
{
    Task<StudentStatisticsDto> GetTotalsAsync(StatisticsQuery ctx, CancellationToken ct);
    Task<IReadOnlyList<CountRow>> GetByGradeAsync(StatisticsQuery ctx, CancellationToken ct);
    Task<IReadOnlyList<ClassCountRow>> GetByClassAsync(StatisticsQuery ctx, CancellationToken ct);

    // Same row set as GetByClassAsync, but aggregated per class in the database with
    // ORDER BY applied before Skip/Take, so pages never materialise the full list.
    // Pages are ordered by (Name, Id); concatenated pages hold the same summaries as
    // the full list (name ties across schools have no stable cross-page order).
    Task<PagedResultDto<ClassCountRow>> GetByClassPagedAsync(
        StatisticsQuery ctx, PaginationQueryDto pagination, CancellationToken ct);

    Task<IReadOnlyList<CountRow>> GetByAgeAsync(StatisticsQuery ctx, CancellationToken ct);

    Task<IReadOnlyList<(DateOnly Month, int Count)>> GetEnrollmentTrendAsync(
        StatisticsQuery ctx, CancellationToken ct);
}
