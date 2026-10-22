using EduCore.Data;
using EduCore.Repositories.Statistics.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories.Statistics;

public class DemographicsStatisticsRepository(AppDbContext context) : IDemographicsStatisticsRepository
{
    private const string NotRecorded = "Not recorded";

    public async Task<IReadOnlyList<(string City, int Schools, int Students, int Teachers, int Classes)>>
        GetByCityAsync(CancellationToken ct)
    {
        var students = context.Students.IgnoreQueryFilters(QueryFilters.SchoolOnly);
        var teachers = context.Teachers.IgnoreQueryFilters(QueryFilters.SchoolOnly);
        var classes = context.Classes.IgnoreQueryFilters(QueryFilters.SchoolOnly);

        var rows = await context.Schools
            .AsNoTracking()
            .Select(s => new
            {
                s.Id,
                CityName = s.City == null ? null : s.City.Name,
                Students = students.Count(st => st.SchoolId == s.Id && st.GraduationDate == null),
                Teachers = teachers.Count(t => t.SchoolId == s.Id),
                Classes = classes.Count(c => c.SchoolId == s.Id)
            })
            .ToListAsync(ct);

        return rows
            .GroupBy(r => string.IsNullOrWhiteSpace(r.CityName) ? NotRecorded : r.CityName!)
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
        var students = context.Students.IgnoreQueryFilters(QueryFilters.SchoolOnly);

        var query = context.Schools.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(s => s.City != null && s.City.Name == city);

        var rows = await query
            .Select(s => new
            {
                CityName = s.City == null ? null : s.City.Name,
                ZoneName = s.Zone == null ? null : s.Zone.Name,
                Students = students.Count(st => st.SchoolId == s.Id && st.GraduationDate == null)
            })
            .ToListAsync(ct);

        return rows
            .GroupBy(r => new
            {
                City = string.IsNullOrWhiteSpace(r.CityName) ? NotRecorded : r.CityName!,
                Zone = string.IsNullOrWhiteSpace(r.ZoneName) ? NotRecorded : r.ZoneName!
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
