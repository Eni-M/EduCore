using EduCore.Enums;

namespace EduCore.Dtos.School;

public record SchoolListDto(Guid Id, string Name, SchoolType Type);
