using EduCore.Dtos.Statistics;
using EduCore.Dtos.Statistics.Aggregate;
using EduCore.Exceptions;
using EduCore.Repositories.Statistics.Interfaces;
using EduCore.Security;
using EduCore.Services.Statistics.Interfaces;

namespace EduCore.Services.Statistics;

public class DemographicsStatisticsService(
    IDemographicsStatisticsRepository demographics,
    ICurrentUser currentUser) : IDemographicsStatisticsService
{
    public async Task<DemographicsOverviewDto> GetOverviewAsync(CancellationToken ct)
    {
        EnsureStateAdmin();

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
        EnsureStateAdmin();

        var zones = await demographics.GetByZoneAsync(city, ct);

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

    private void EnsureStateAdmin()
    {
        if (!currentUser.IsStateAdmin)
            throw new ForbiddenException(
                "Demographic statistics span every school and are state administrator only.");
    }
}
