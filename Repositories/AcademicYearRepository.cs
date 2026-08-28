using EduCore.Data;
using EduCore.Dtos.AcademicYear;
using EduCore.Helpers;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class AcademicYearRepository(AppDbContext context)
    : Repository<AcademicYear>(context), IAcademicYearRepository
{
    public async Task<IEnumerable<AcademicYear>> GetAllOrderedAsync(CancellationToken ct) =>
        await _context.AcademicYears
            .AsNoTracking()
            .OrderByDescending(a => a.StartDate)
            .ToListAsync(ct);

    public async Task<IEnumerable<AcademicYearListDto>> GetAllAcrossSchoolsAsync(
        Guid? schoolId, CancellationToken ct)
    {
        var query = _context.AcademicYears.AsNoTracking().IgnoreQueryFilters();

        if (schoolId is not null)
            query = query.Where(a => a.SchoolId == schoolId);

        return await (
                from a in query
                join s in _context.Schools on a.SchoolId equals s.Id
                orderby a.StartDate descending, s.Name
                select new AcademicYearListDto(a.Id, a.Name, a.StartDate, a.EndDate, s.Name))
            .ToListAsync(ct);
    }

    public async Task<bool> NameExistsAsync(string name, CancellationToken ct) =>
        await _context.AcademicYears.AnyAsync(a => a.Name.ToLower() == name.ToLower(), ct);

    public async Task<bool> OverlapsAsync(DateOnly startDate, DateOnly endDate, CancellationToken ct) =>
        await _context.AcademicYears.AnyAsync(a => a.StartDate <= endDate && startDate <= a.EndDate, ct);

    public async Task<AcademicYear?> GetByIdAcrossSchoolsAsync(Guid academicYearId, CancellationToken ct) =>
        await _context.AcademicYears
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.Id == academicYearId, ct);

    // Which year a report defaults to. The choosing is AcademicYearPicker's -- see there for
    // why "the most recent year" is the one that most recently ENDED, not the one with the
    // latest start date.
    public async Task<AcademicYear?> GetCurrentAsync(Guid schoolId, DateOnly onDate, CancellationToken ct) =>
        AcademicYearPicker.CurrentOn(
            await _context.AcademicYears
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(a => a.SchoolId == schoolId)
                .ToListAsync(ct),
            onDate);
}
