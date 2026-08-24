namespace EduCore.Dtos.Class;

// SchoolId isn't here -- it comes from the caller's own school context, not client input.
public record ClassCreateDto(string Name, Guid AcademicYearId, int Grade);
