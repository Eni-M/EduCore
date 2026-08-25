using EduCore.Data;
using EduCore.Dtos.School;
using EduCore.DTOs.School;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class SchoolRepository(AppDbContext context) : Repository<School>(context), ISchoolRepository
{
    public async Task<IEnumerable<School>> GetAllOrderedAsync(CancellationToken ct) =>
        await _context.Schools
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

    public async Task<bool> NameExistsAsync(string name, Guid? excludeSchoolId, CancellationToken ct) =>
        await _context.Schools.AnyAsync(
            s => s.Name.ToLower() == name.ToLower() && (excludeSchoolId == null || s.Id != excludeSchoolId),
            ct);


    public async Task<IEnumerable<SchoolStatisticsDto>> GetStatisticsAsync(CancellationToken ct) =>
    await _context.Schools
        .AsNoTracking()
        .IgnoreQueryFilters()
        .OrderBy(s => s.Name)
        .Select(s => new SchoolStatisticsDto(
            s.Id,
            s.Name,
            s.Type,
            _context.Admins.Count(a => a.SchoolId == s.Id),
            _context.Teachers.Count(t => t.SchoolId == s.Id),
            _context.Students.Count(st => st.SchoolId == s.Id),
            _context.Students.Count(st => st.SchoolId == s.Id && st.GraduationDate == null),
            _context.Classes.Count(c => c.SchoolId == s.Id),
            _context.Subjects.Count(su => su.SchoolId == s.Id)))
        .ToListAsync(ct);

   
}
