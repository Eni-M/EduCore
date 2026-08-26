using EduCore.Enums;

namespace EduCore.Dtos.School;

public record SchoolCreateDto(string Name, SchoolType Type, string? City, string? Zone);
