using EduCore.Dtos.Pagination;
using EduCore.Dtos.Statistics;
using EduCore.Dtos.Statistics.Rows;
using EduCore.Dtos.Statistics.Individual;
using EduCore.Services.Statistics;

namespace EduCore.Repositories.Statistics.Interfaces;

public interface IAttendanceStatisticsRepository
{
    Task<AttendanceTotals> GetTotalsAsync(StatisticsQuery ctx, CancellationToken ct);
    Task<IReadOnlyList<AttendanceGroup>> GetByClassAsync(StatisticsQuery ctx, CancellationToken ct);

    // Same aggregation as GetByClassAsync, restricted to the requested classes so a
    // paged caller only pays for the page it returns.
    Task<IReadOnlyList<AttendanceGroup>> GetByClassAsync(
        StatisticsQuery ctx, IReadOnlyCollection<Guid> classIds, CancellationToken ct);
    Task<IReadOnlyList<AttendanceGroup>> GetByGradeAsync(StatisticsQuery ctx, CancellationToken ct);
    Task<IReadOnlyList<AttendanceGroup>> GetBySubjectAsync(StatisticsQuery ctx, CancellationToken ct);

    // Same aggregations, ordered totally and sliced in the database.
    Task<PagedResultDto<AttendanceGroup>> GetByClassPagedAsync(
        StatisticsQuery ctx, PaginationQueryDto pagination, CancellationToken ct);
    Task<PagedResultDto<AttendanceGroup>> GetBySubjectPagedAsync(
        StatisticsQuery ctx, PaginationQueryDto pagination, CancellationToken ct);

    Task<IReadOnlyList<(DateOnly Day, int Records, int Present)>> GetDailyTrendAsync(
        StatisticsQuery ctx, CancellationToken ct);

    Task<IReadOnlyList<StudentAttendanceSummaryDto>> GetByStudentAsync(
        StatisticsQuery ctx, CancellationToken ct);

    // Same students as GetByStudentAsync, grouped and sliced in the database.
    Task<PagedResultDto<StudentAttendanceSummaryDto>> GetByStudentPagedAsync(
        StatisticsQuery ctx, PaginationQueryDto pagination, CancellationToken ct);

    Task<IReadOnlyList<ExcessiveAbsenceStudentDto>> GetExcessiveAbsencesAsync(
        StatisticsQuery ctx, double absenceRateThreshold, CancellationToken ct);

    // Same students as GetExcessiveAbsencesAsync, filtered/ordered per page in the
    // database: concatenated pages hold the same students.
    Task<PagedResultDto<ExcessiveAbsenceStudentDto>> GetExcessiveAbsencesPagedAsync(
        StatisticsQuery ctx,
        double absenceRateThreshold,
        PaginationQueryDto pagination,
        CancellationToken ct);
}
