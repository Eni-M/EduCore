using EduCore.Data;
using EduCore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EduCore.Data;

// Counts writes that would change a school's statistics, so StatisticsSnapshotService
// can tell a stale snapshot from a current one without re-running the aggregates to find out.
//
// The count is deliberately per school and across every contributing table, not just
// enrollments: a term's worth of attendance or marks moves the figures every bit as much as a
// new student does, and a snapshot that only noticed enrollments would sit wrong all term.
//
// The counter is bumped after the write commits, in a separate statement. Doing it inside the
// same SaveChanges would mean loading and tracking snapshot rows on every unrelated write; the
// extra round trip is paid only by writes, which are far rarer here than reads.
public class StatisticsInvalidationInterceptor : SaveChangesInterceptor
{
    // The tables every snapshotted figure is derived from. A write to anything else — a notice,
    // a document, a calendar event — cannot move a statistic, so it must not age a snapshot.
    private static bool Contributes(object entity) => entity
        is Student or Enrollment or Attendance or Mark or DiaryEntry
        or Class or Teacher or ClassSubjectTeacher;

    // Collected in SavingChanges (while the change tracker still knows what changed) and
    // applied in SavedChanges (once the write is known to have succeeded).
    private readonly Dictionary<DbContext, Dictionary<Guid, int>> _pending = new();

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        Collect(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        Collect(eventData.Context);
        return base.SavingChangesAsync(eventData, result, ct);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        var counts = Take(eventData.Context);

        if (counts is not null)
            Apply(eventData.Context!, counts);

        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken ct = default)
    {
        var counts = Take(eventData.Context);

        if (counts is not null)
            await ApplyAsync(eventData.Context!, counts, ct);

        return await base.SavedChangesAsync(eventData, result, ct);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData) =>
        Take(eventData.Context);

    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData, CancellationToken ct = default)
    {
        Take(eventData.Context);
        return Task.CompletedTask;
    }

    private void Collect(DbContext? context)
    {
        if (context is null)
            return;

        var counts = new Dictionary<Guid, int>();

        foreach (var entry in context.ChangeTracker.Entries<ISchoolScoped>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;

            if (!Contributes(entry.Entity))
                continue;

            // A move between schools ages both: the old school loses the row, the new one gains it.
            var schoolIds = entry.State == EntityState.Modified
                ? new[]
                {
                    entry.Property(nameof(ISchoolScoped.SchoolId)).OriginalValue as Guid? ?? entry.Entity.SchoolId,
                    entry.Entity.SchoolId
                }.Distinct()
                : [entry.Entity.SchoolId];

            foreach (var schoolId in schoolIds)
                counts[schoolId] = counts.GetValueOrDefault(schoolId) + 1;
        }

        if (counts.Count > 0)
            _pending[context] = counts;
    }

    private Dictionary<Guid, int>? Take(DbContext? context)
    {
        if (context is null || !_pending.Remove(context, out var counts))
            return null;

        return counts;
    }

    private static void Apply(DbContext context, Dictionary<Guid, int> counts)
    {
        foreach (var (schoolId, delta) in counts)
            Query(context, schoolId)
                .ExecuteUpdate(s => s.SetProperty(
                    x => x.ChangesSinceComputed, x => x.ChangesSinceComputed + delta));
    }

    private static async Task ApplyAsync(
        DbContext context, Dictionary<Guid, int> counts, CancellationToken ct)
    {
        foreach (var (schoolId, delta) in counts)
            await Query(context, schoolId)
                .ExecuteUpdateAsync(s => s.SetProperty(
                    x => x.ChangesSinceComputed, x => x.ChangesSinceComputed + delta), ct);
    }

    // IgnoreQueryFilters: the write may have been made by someone with no claim on this school
    // (a regional admin moving a teacher, the seeder, a background job), and the counter must
    // still be bumped. It exposes nothing — the statement only increments a number.
    private static IQueryable<SchoolStatisticsSnapshot> Query(DbContext context, Guid schoolId) =>
        context.Set<SchoolStatisticsSnapshot>()
            .IgnoreQueryFilters(QueryFilters.SchoolOnly)
            .Where(x => x.SchoolId == schoolId);
}
