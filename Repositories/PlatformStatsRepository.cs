using EduCore.Data;
using EduCore.Dtos.Public;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class PlatformStatsRepository(AppDbContext context) : IPlatformStatsRepository
{
    // IgnoreQueryFilters throughout: the caller is anonymous, so the school-scope filters would
    // match nothing. Safe here only because every figure is a whole-platform count -- add
    // anything narrower and it needs the filters back.
    public async Task<PlatformStatsDto> GetAsync(CancellationToken ct) =>
        new(
            Schools: await context.Schools.IgnoreQueryFilters(QueryFilters.SchoolOnly)
                .CountAsync(s => s.DeactivatedAt == null, ct),

            // Cities that actually hold a school, not every row in the geography table --
            // otherwise the figure counts reference data rather than reach.
            Cities: await context.Schools.IgnoreQueryFilters(QueryFilters.SchoolOnly)
                .Where(s => s.DeactivatedAt == null && s.CityId != null)
                .Select(s => s.CityId)
                .Distinct()
                .CountAsync(ct),

            Students: await context.Students.IgnoreQueryFilters(QueryFilters.SchoolOnly).CountAsync(ct),
            Teachers: await context.Teachers.IgnoreQueryFilters(QueryFilters.SchoolOnly).CountAsync(ct),

            // A diary entry is one lesson a teacher recorded as taught.
            Lessons: await context.DiaryEntries.IgnoreQueryFilters(QueryFilters.SchoolOnly).CountAsync(ct),
            Attendances: await context.Attendances.IgnoreQueryFilters(QueryFilters.SchoolOnly).CountAsync(ct));
}
