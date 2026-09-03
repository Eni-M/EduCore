namespace EduCore.Dtos.Class;

public record ClassCreateDto(string Name, Guid AcademicYearId, int Grade, int? Capacity, Guid? ShiftId);
