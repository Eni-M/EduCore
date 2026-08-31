namespace EduCore.Dtos.Statistics.Aggregate;

public record CityDemographicsDto(
    string City,
    int Schools,
    int Students,
    int Teachers,
    int Classes,
    Measure ShareOfStudents);

public record ZoneDemographicsDto(
    string City,
    string Zone,
    int Schools,
    int Students,
    Measure ShareOfCityStudents);

public record DemographicsOverviewDto(
    int Cities,
    int Zones,
    int Schools,
    int Students,
    IReadOnlyList<CityDemographicsDto> ByCity);
