using EduCore.Dtos.Statistics;
using EduCore.Dtos.Statistics.Rows;
using EduCore.Dtos.Statistics.Individual;
using EduCore.Services.Statistics;

namespace EduCore.Repositories.Statistics.Interfaces;

public interface IAttendanceStatisticsRepository
{
    Task<AttendanceTotals> GetTotalsAsync(StatisticsQuery ctx, CancellationToken ct);
    Task<IReadOnlyList<AttendanceGroup>> GetByClassAsync(StatisticsQuery ctx, CancellationToken ct);
    Task<IReadOnlyList<AttendanceGroup>> GetByGradeAsync(StatisticsQuery ctx, CancellationToken ct);
    Task<IReadOnlyList<AttendanceGroup>> GetBySubjectAsync(StatisticsQuery ctx, CancellationToken ct);

    Task<IReadOnlyList<(DateOnly Day, int Records, int Present)>> GetDailyTrendAsync(
        StatisticsQuery ctx, CancellationToken ct);

    Task<IReadOnlyList<StudentAttendanceSummaryDto>> GetByStudentAsync(
        StatisticsQuery ctx, CancellationToken ct);

    Task<IReadOnlyList<ExcessiveAbsenceStudentDto>> GetExcessiveAbsencesAsync(
        StatisticsQuery ctx, double absenceRateThreshold, CancellationToken ct);
}
