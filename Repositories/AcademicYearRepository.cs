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
    public async Task<IEnumerable<AcademicYearListDto>> GetAllAcrossSchoolsAsync(
        Guid? schoolId, Guid? cityId, CancellationToken ct)
    {
        var query = _context.AcademicYears.AsNoTracking().IgnoreQueryFilters();

        if (schoolId is not null)
            query = query.Where(a => a.SchoolId == schoolId);

        return await (
                from a in query
                join s in _context.Schools on a.SchoolId equals s.Id
                where cityId == null || s.CityId == cityId
                orderby a.StartDate descending, s.Name
                select new AcademicYearListDto(a.Id, a.Name, a.StartDate, a.EndDate, s.Name))
            .ToListAsync(ct);
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
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.Id == academicYearId, ct);

    public async Task<AcademicYear?> GetCurrentAsync(Guid schoolId, DateOnly onDate, CancellationToken ct) =>
        AcademicYearPicker.CurrentOn(
            await _context.AcademicYears
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(a => a.SchoolId == schoolId)
                .ToListAsync(ct),
            onDate);
}
