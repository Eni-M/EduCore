using EduCore.Enums;

namespace EduCore.Dtos.School;

public record SchoolEditDto(string Name, SchoolType Type, Guid? CityId, Guid? ZoneId);
