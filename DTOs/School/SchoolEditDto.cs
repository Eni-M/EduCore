using EduCore.Enums;

namespace EduCore.Dtos.School;

public record SchoolEditDto(string Name, SchoolType Type, string? City, string? Zone);
