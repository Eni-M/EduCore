namespace EduCore.Dtos.Statistics.Aggregate;

// Where the students actually are. City and Zone are nullable on School, so schools with no
// recorded location are reported under "Not recorded" rather than dropped -- a demographic
// report that quietly omits schools is worse than one that admits a gap.
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
