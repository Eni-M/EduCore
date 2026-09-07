using EduCore.Dtos.Statistics;
using EduCore.Models;

namespace EduCore.Repositories.Statistics.Interfaces;

// Identity of one snapshot: which school, over which window, under which year/period filter.
public record SnapshotKey(
    Guid SchoolId,
    DateOnly From,
    DateOnly To,
    Guid? AcademicYearId,
    Guid? GradingPeriodId);

public interface IStatisticsSnapshotRepository
{
    Task<SchoolStatisticsSnapshot?> GetAsync(SnapshotKey key, CancellationToken ct);

    // Every school's snapshot for one window, in a single statement. This is what replaces the
    // platform overview's per-school loop.
    Task<IReadOnlyList<SchoolStatisticsSnapshot>> GetManyAsync(
        IReadOnlyCollection<Guid> schoolIds,
        DateOnly from,
        DateOnly to,
        Guid? academicYearId,
        Guid? gradingPeriodId,
        CancellationToken ct);

    // Classes in scope. The other figures come from the same repositories the live path uses,
    // so they agree by construction; this one has no existing home.
    Task<int> CountClassesAsync(StatisticsQuery ctx, CancellationToken ct);

    Task SaveAsync(SchoolStatisticsSnapshot snapshot, CancellationToken ct);
}
