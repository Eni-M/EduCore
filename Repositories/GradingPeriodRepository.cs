using EduCore.Data;
using EduCore.Dtos.GradingPeriod;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class GradingPeriodRepository(AppDbContext context)
    : Repository<GradingPeriod>(context), IGradingPeriodRepository
{
    public async Task<IEnumerable<GradingPeriodListDto>> GetByAcademicYearAsync(
        Guid academicYearId,
        CancellationToken ct) =>
        await (from g in _context.GradingPeriods.AsNoTracking()
               join y in _context.AcademicYears on g.AcademicYearId equals y.Id
               where g.AcademicYearId == academicYearId
               orderby g.StartDate
               select new GradingPeriodListDto(g.Id, g.AcademicYearId, y.Name, g.Name, g.StartDate, g.EndDate))
            .ToListAsync(ct);

    public async Task<bool> NameExistsInYearAsync(Guid academicYearId, string name, CancellationToken ct) =>
        await _context.GradingPeriods.AnyAsync(
            g => g.AcademicYearId == academicYearId && g.Name.ToLower() == name.ToLower(),
            ct);

    public async Task<GradingPeriod?> GetByIdAcrossSchoolsAsync(Guid gradingPeriodId, CancellationToken ct) =>
        await _context.GradingPeriods
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.Id == gradingPeriodId, ct);
}
