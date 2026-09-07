using EduCore.Dtos.Statistics;
using EduCore.Models;

namespace EduCore.Services.Statistics.Interfaces;

public interface IStatisticsSnapshotService
{
    // Can this query be served from a snapshot at all? False for cross-school queries and for
    // anything narrowed by class, grade, subject, teacher or student origin.
    bool IsSnapshotable(StatisticsQuery ctx);

    // The current snapshot for this query, recomputing first if it has gone stale. Null when
    // the query is not snapshotable or the feature is switched off, which means "compute live".
    Task<SchoolStatisticsSnapshot?> GetAsync(StatisticsQuery ctx, CancellationToken ct);

    // Snapshots for many schools at once, refreshing only those that are stale. Backs the
    // platform overview, which previously recomputed every school on every request.
    Task<IReadOnlyDictionary<Guid, SchoolStatisticsSnapshot>> GetManyAsync(
        StatisticsQuery ctx, IReadOnlyCollection<Guid> schoolIds, CancellationToken ct);
}
