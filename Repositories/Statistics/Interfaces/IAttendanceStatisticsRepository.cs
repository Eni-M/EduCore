using EduCore.Dtos.Statistics.Individual;
using EduCore.Services.Statistics;

namespace EduCore.Repositories.Statistics.Interfaces;

public interface IAttendanceStatisticsRepository
{
    Task<AttendanceTotals> GetTotalsAsync(StatisticsContext ctx, CancellationToken ct);
    Task<IReadOnlyList<AttendanceGroup>> GetByClassAsync(StatisticsContext ctx, CancellationToken ct);
    Task<IReadOnlyList<AttendanceGroup>> GetByGradeAsync(StatisticsContext ctx, CancellationToken ct);
    Task<IReadOnlyList<AttendanceGroup>> GetBySubjectAsync(StatisticsContext ctx, CancellationToken ct);

    Task<IReadOnlyList<(DateOnly Day, int Records, int Present)>> GetDailyTrendAsync(
        StatisticsContext ctx, CancellationToken ct);

    Task<IReadOnlyList<StudentAttendanceSummaryDto>> GetByStudentAsync(
        StatisticsContext ctx, CancellationToken ct);

    // Students whose unexcused absence rate is at or above the given threshold.
    Task<IReadOnlyList<ExcessiveAbsenceStudentDto>> GetExcessiveAbsencesAsync(
        StatisticsContext ctx, double absenceRateThreshold, CancellationToken ct);
}
