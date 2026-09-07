using EduCore.Dtos.Pagination;
using EduCore.Dtos.Statistics;
using EduCore.Dtos.Statistics.Aggregate;
using EduCore.Dtos.Statistics.Individual;

namespace EduCore.Services.Statistics.Interfaces;

public interface ITeacherStatisticsService
{
    Task<TeacherStatisticsDto> GetSummaryAsync(StatisticsFilter filter, CancellationToken ct);
    Task<IReadOnlyList<TeacherWorkloadDto>> GetWorkloadAsync(StatisticsFilter filter, CancellationToken ct);
    Task<IReadOnlyList<TeacherCoverageDto>> GetCoverageAsync(StatisticsFilter filter, CancellationToken ct);

    // Same rows, sliced in the database.
    Task<PagedResultDto<TeacherWorkloadDto>> GetWorkloadPagedAsync(
        StatisticsFilter filter, PaginationQueryDto pagination, CancellationToken ct);
    Task<PagedResultDto<TeacherCoverageDto>> GetCoveragePagedAsync(
        StatisticsFilter filter, PaginationQueryDto pagination, CancellationToken ct);
    Task<TeacherDetailStatisticsDto> GetDetailAsync(
        StatisticsFilter filter, Guid teacherId, CancellationToken ct);
}
