using EduCore.Data;
using EduCore.Repositories.Statistics.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories.Statistics;

// Demographics work across schools by definition -- "which city has the most students" is not
// a question a single school can ask about itself. The service layer therefore restricts these
// to SuperAdmin, and the queries deliberately span every school.
//
// School is not ISchoolScoped (it is the tenant root, not a tenant member), so these do not go
// through StatisticsRepositoryBase.Scope. The per-school tables they join do, which is why
// each one drops the global filter explicitly.
public class DemographicsStatisticsRepository(AppDbContext context) : IDemographicsStatisticsRepository
{
    private const string NotRecorded = "Not recorded";

    public async Task<IReadOnlyList<(string City, int Schools, int Students, int Teachers, int Classes)>>
        GetByCityAsync(CancellationToken ct)
    {
        var students = context.Students.IgnoreQueryFilters();
        var teachers = context.Teachers.IgnoreQueryFilters();
        var classes = context.Classes.IgnoreQueryFilters();

        var rows = await context.Schools
            .AsNoTracking()
            .Select(s => new
            {
                s.Id,
                s.City,
                Students = students.Count(st => st.SchoolId == s.Id && st.GraduationDate == null),
                Teachers = teachers.Count(t => t.SchoolId == s.Id),
                Classes = classes.Count(c => c.SchoolId == s.Id)
            })
            .ToListAsync(ct);

        // Grouped in memory: this is one row per school, and doing it here lets null and empty
        // city names collapse into the same "Not recorded" bucket without a SQL COALESCE.
        return rows
            .GroupBy(r => string.IsNullOrWhiteSpace(r.City) ? NotRecorded : r.City!)
            .Select(g => (
                City: g.Key,
                Schools: g.Count(),
                Students: g.Sum(r => r.Students),
                Teachers: g.Sum(r => r.Teachers),
                Classes: g.Sum(r => r.Classes)))
            .OrderByDescending(r => r.Students)
            .ThenBy(r => r.City)
            .ToList();
    }

    public async Task<IReadOnlyList<(string City, string Zone, int Schools, int Students)>>
        GetByZoneAsync(string? city, CancellationToken ct)
    {
        var students = context.Students.IgnoreQueryFilters();

        var query = context.Schools.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(s => s.City == city);

        var rows = await query
            .Select(s => new
            {
                s.City,
                s.Zone,
                Students = students.Count(st => st.SchoolId == s.Id && st.GraduationDate == null)
            })
            .ToListAsync(ct);

        return rows
            .GroupBy(r => new
            {
                City = string.IsNullOrWhiteSpace(r.City) ? NotRecorded : r.City!,
                Zone = string.IsNullOrWhiteSpace(r.Zone) ? NotRecorded : r.Zone!
            })
            .Select(g => (
                g.Key.City,
                g.Key.Zone,
                Schools: g.Count(),
                Students: g.Sum(r => r.Students)))
            .OrderBy(r => r.City)
            .ThenByDescending(r => r.Students)
            .ToList();
    }
}
