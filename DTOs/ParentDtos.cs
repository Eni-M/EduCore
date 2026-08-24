namespace EduCore.Dtos;

public record ParentListDto(Guid Id, string Name, string PhoneNumber);

public record ParentCreateDto(Guid StudentId, string Name, string PhoneNumber);

public record ParentEditDto(string Name, string PhoneNumber);
