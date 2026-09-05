using EduCore.Dtos.Pagination;
using EduCore.Dtos.Statistics;
using EduCore.Dtos.Statistics.Aggregate;
using EduCore.Dtos.Statistics.Individual;

namespace EduCore.Services.Statistics.Interfaces;

public interface IAttendanceStatisticsService
{
    Task<AttendanceStatisticsDto> GetSummaryAsync(StatisticsFilter filter, CancellationToken ct);
    Task<IReadOnlyList<AttendanceByKeyDto>> GetByClassAsync(StatisticsFilter filter, CancellationToken ct);
    Task<IReadOnlyList<AttendanceByKeyDto>> GetByGradeAsync(StatisticsFilter filter, CancellationToken ct);
    Task<IReadOnlyList<AttendanceByKeyDto>> GetBySubjectAsync(StatisticsFilter filter, CancellationToken ct);

    // Same aggregations, sliced in the database.
    Task<PagedResultDto<AttendanceByKeyDto>> GetByClassPagedAsync(
        StatisticsFilter filter, PaginationQueryDto pagination, CancellationToken ct);
    Task<PagedResultDto<AttendanceByKeyDto>> GetBySubjectPagedAsync(
        StatisticsFilter filter, PaginationQueryDto pagination, CancellationToken ct);
    Task<IReadOnlyList<TrendPointDto>> GetTrendAsync(StatisticsFilter filter, CancellationToken ct);
    Task<IReadOnlyList<StudentAttendanceSummaryDto>> GetByStudentAsync(StatisticsFilter filter, CancellationToken ct);

    // Same students as GetByStudentAsync, grouped and sliced in the database.
    Task<PagedResultDto<StudentAttendanceSummaryDto>> GetByStudentPagedAsync(
        StatisticsFilter filter, PaginationQueryDto pagination, CancellationToken ct);
    Task<IReadOnlyList<ExcessiveAbsenceStudentDto>> GetExcessiveAbsencesAsync(
        StatisticsFilter filter, double? thresholdPercent, CancellationToken ct);

    // Same students as GetExcessiveAbsencesAsync, filtered/ordered per page in the database.
    Task<PagedResultDto<ExcessiveAbsenceStudentDto>> GetExcessiveAbsencesPagedAsync(
        StatisticsFilter filter,
        double? thresholdPercent,
        PaginationQueryDto pagination,
        CancellationToken ct);
}
