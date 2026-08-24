namespace EduCore.Dtos;

public record SubjectListDto(Guid Id, string Name);

public record SubjectCreateDto(string Name);

public record SubjectEditDto(string Name);
