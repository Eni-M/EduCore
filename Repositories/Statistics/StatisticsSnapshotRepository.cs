using EduCore.Data;
using EduCore.Dtos.Statistics;
using EduCore.Models;
using EduCore.Repositories.Statistics.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories.Statistics;

public class StatisticsSnapshotRepository(AppDbContext context)
    : StatisticsRepositoryBase(context), IStatisticsSnapshotRepository
{
    // IgnoreQueryFilters throughout, for the same reason every other statistics query does it:
    // a state or regional admin holds no school claim, so the school-scope filter would hide
    // exactly the rows they are entitled to read. The scope is applied explicitly instead —
    // callers pass school ids the scope resolver already authorised.
    private IQueryable<SchoolStatisticsSnapshot> Snapshots =>
        _context.SchoolStatisticsSnapshots.IgnoreQueryFilters();

    public Task<SchoolStatisticsSnapshot?> GetAsync(SnapshotKey key, CancellationToken ct) =>
        Snapshots.FirstOrDefaultAsync(
            s => s.SchoolId == key.SchoolId
                 && s.From == key.From
                 && s.To == key.To
                 && s.AcademicYearId == key.AcademicYearId
                 && s.GradingPeriodId == key.GradingPeriodId,
            ct);

    public async Task<IReadOnlyList<SchoolStatisticsSnapshot>> GetManyAsync(
        IReadOnlyCollection<Guid> schoolIds,
        DateOnly from,
        DateOnly to,
        Guid? academicYearId,
        Guid? gradingPeriodId,
        CancellationToken ct) =>
        await Snapshots
            .AsNoTracking()
            .Where(s => schoolIds.Contains(s.SchoolId)
                        && s.From == from
                        && s.To == to
                        && s.AcademicYearId == academicYearId
                        && s.GradingPeriodId == gradingPeriodId)
            .ToListAsync(ct);

    public Task<int> CountClassesAsync(StatisticsQuery ctx, CancellationToken ct) =>
        ClassesInScope(ctx).CountAsync(ct);

    public async Task SaveAsync(SchoolStatisticsSnapshot snapshot, CancellationToken ct)
    {
        if (snapshot.Id == Guid.Empty)
        {
            snapshot.Id = Guid.NewGuid();
            await _context.SchoolStatisticsSnapshots.AddAsync(snapshot, ct);
        }
        else
        {
            _context.SchoolStatisticsSnapshots.Update(snapshot);
        }

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Another request refreshed the same (school, window) first and won the unique
            // index. Both computed the same numbers from the same data, so the row that landed
            // is as good as this one — drop ours rather than failing the read that triggered it.
            _context.Entry(snapshot).State = EntityState.Detached;
        }
    }
}
