namespace EduCore.Dtos.Location;

// CityCount is carried so the management screen can warn before a delete that would orphan
// cities, without a second request per row.
public record StateListDto(Guid Id, string Name, int CityCount);
