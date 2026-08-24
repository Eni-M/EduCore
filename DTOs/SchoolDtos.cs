namespace EduCore.Dtos;

public record SchoolListDto(Guid Id, string Name, string Type);

public record SchoolCreateDto(string Name, string Type);

public record SchoolEditDto(string Name, string Type);
