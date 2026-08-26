namespace EduCore.Repositories.Statistics.Interfaces;

public interface IDemographicsStatisticsRepository
{
    Task<IReadOnlyList<(string City, int Schools, int Students, int Teachers, int Classes)>>
        GetByCityAsync(CancellationToken ct);

    Task<IReadOnlyList<(string City, string Zone, int Schools, int Students)>>
        GetByZoneAsync(string? city, CancellationToken ct);
}
