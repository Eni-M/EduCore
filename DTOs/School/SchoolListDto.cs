using EduCore.Enums;

namespace EduCore.Dtos.School;

public record SchoolListDto(
    Guid Id,
    string Name,
    SchoolType Type,
    Guid? CityId,
    string? CityName,
    Guid? ZoneId,
    string? ZoneName,
    string? StateName,
    DateTime? DeactivatedAt);
