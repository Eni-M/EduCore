using EduCore.Dtos.Statistics;
using EduCore.Dtos.Statistics.Aggregate;
using EduCore.Exceptions;
using EduCore.Repositories.Statistics.Interfaces;
using EduCore.Security;
using EduCore.Services.Statistics.Interfaces;

namespace EduCore.Services.Statistics;

// SuperAdmin only, and not because of squeamishness: "which city has the most students" is a
// question about the whole network, and a single school answering it about itself would be
// meaningless. Guarded here as well as at the endpoint, so the rule holds even if the route
// is later remapped.
// Every figure here is an exact headcount, or a share sitting beside the counts it was
// derived from, so there is nothing for CellSuppressor to protect -- see its header.
public class DemographicsStatisticsService(
    IDemographicsStatisticsRepository demographics,
    ICurrentUser currentUser) : IDemographicsStatisticsService
{
    public async Task<DemographicsOverviewDto> GetOverviewAsync(CancellationToken ct)
    {
        EnsureSuperAdmin();

        var cities = await demographics.GetByCityAsync(ct);
        var zones = await demographics.GetByZoneAsync(null, ct);

        var totalStudents = cities.Sum(c => c.Students);

        var byCity = cities
            .Select(c => new CityDemographicsDto(
                c.City,
                c.Schools,
                c.Students,
                c.Teachers,
                c.Classes,
                Measures.Rate(c.Students, totalStudents)))
            .ToList();

        return new DemographicsOverviewDto(
            cities.Count,
            zones.Count,
            cities.Sum(c => c.Schools),
            totalStudents,
            byCity);
    }

    public async Task<IReadOnlyList<ZoneDemographicsDto>> GetByZoneAsync(
        string? city, CancellationToken ct)
    {
        EnsureSuperAdmin();

        var zones = await demographics.GetByZoneAsync(city, ct);

        // Each zone's share is of its own city's students, not of the whole country -- a zone
        // holding 40% of its city is the useful figure, not that it holds 0.3% nationally.
        var cityTotals = zones
            .GroupBy(z => z.City)
            .ToDictionary(g => g.Key, g => g.Sum(z => z.Students));

        return zones
            .Select(z => new ZoneDemographicsDto(
                z.City,
                z.Zone,
                z.Schools,
                z.Students,
                Measures.Rate(z.Students, cityTotals.GetValueOrDefault(z.City))))
            .ToList();
    }

    private void EnsureSuperAdmin()
    {
        if (!currentUser.IsSuperAdmin)
            throw new ForbiddenException("Demographic statistics span every school and are SuperAdmin only.");
    }
}
