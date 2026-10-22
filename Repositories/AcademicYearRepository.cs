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

    // For callers who hold no school of their own -- the ministry, and a regional admin, whose
    // tokens carry no school_id claim at all. cityId narrows a regional admin to their own city;
    // without it they would read the whole country.
    //
    // An academic year is stored once per school, so 2026/2027 exists eighty times over with
    // identical dates. Naming a school is only meaningful when one was asked for; otherwise the
    // year is reported once, as the single national year it actually is. A picker listing
    // 2026/2027 eighty times is no picker at all.
    public async Task<IEnumerable<AcademicYearListDto>> GetAllAcrossSchoolsAsync(
        Guid? schoolId, Guid? cityId, CancellationToken ct)
    {
        var query = _context.AcademicYears.AsNoTracking().IgnoreQueryFilters(QueryFilters.SchoolOnly);

        if (schoolId is not null)
            query = query.Where(a => a.SchoolId == schoolId);

        var rows = await (
                from a in query
                join s in _context.Schools on a.SchoolId equals s.Id
                where cityId == null || s.CityId == cityId
                select new { a.Id, a.Name, a.StartDate, a.EndDate, SchoolName = s.Name })
            .ToListAsync(ct);

        if (schoolId is not null)
            return rows
                .OrderByDescending(r => r.StartDate).ThenBy(r => r.SchoolName)
                .Select(r => new AcademicYearListDto(r.Id, r.Name, r.StartDate, r.EndDate, r.SchoolName))
                .ToList();

        // Grouped here rather than in SQL: MAX over a uuid is not something every provider
        // translates, and this is a few hundred narrow rows for a picker.
        // in-memory group over the whole year table; push to SQL if it ever grows.
        // Two schools whose 2026/2027 genuinely runs different dates stay separate entries --
        // that is a real difference, not noise to hide.
        return rows
            .GroupBy(r => new { r.Name, r.StartDate, r.EndDate })
            .OrderByDescending(g => g.Key.StartDate).ThenBy(g => g.Key.Name)
            .Select(g => new AcademicYearListDto(
                g.First().Id, g.Key.Name, g.Key.StartDate, g.Key.EndDate, null))
            .ToList();
    }

    // exceptId lets an edit compare against every year but the one being edited, which would
    // otherwise always collide with itself.
    public async Task<bool> NameExistsAsync(string name, Guid? exceptId, CancellationToken ct) =>
        await _context.AcademicYears.AnyAsync(
            a => a.Name.ToLower() == name.ToLower() && (exceptId == null || a.Id != exceptId), ct);

    public async Task<bool> OverlapsAsync(
        DateOnly startDate, DateOnly endDate, Guid? exceptId, CancellationToken ct) =>
        await _context.AcademicYears.AnyAsync(
            a => a.StartDate <= endDate && startDate <= a.EndDate && (exceptId == null || a.Id != exceptId), ct);

    public async Task<AcademicYear?> GetByIdAcrossSchoolsAsync(Guid academicYearId, CancellationToken ct) =>
        await _context.AcademicYears
            .AsNoTracking()
            .IgnoreQueryFilters(QueryFilters.SchoolOnly)
            .FirstOrDefaultAsync(a => a.Id == academicYearId, ct);

    public async Task<AcademicYear?> GetCurrentAsync(Guid schoolId, DateOnly onDate, CancellationToken ct) =>
        AcademicYearPicker.CurrentOn(
            await _context.AcademicYears
                .AsNoTracking()
                .IgnoreQueryFilters(QueryFilters.SchoolOnly)
                .Where(a => a.SchoolId == schoolId)
                .ToListAsync(ct),
            onDate);
}
