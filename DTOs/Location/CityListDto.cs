namespace EduCore.Dtos.Location;

// StateName travels with every city: a bare city name is ambiguous in a picker, and the
// caller would otherwise have to join the two lists itself.
public record CityListDto(Guid Id, string Name, Guid StateId, string StateName);
