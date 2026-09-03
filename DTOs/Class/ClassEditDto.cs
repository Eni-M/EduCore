namespace EduCore.Dtos.Class;

public record ClassEditDto(string Name, int Grade, int? Capacity, Guid? ShiftId);
