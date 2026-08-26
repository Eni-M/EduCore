namespace EduCore.Dtos.Class;

// SchoolId isn't here -- it comes from the caller's own school context, not client input.
// Capacity is optional: only classes that declare one get capacity-vs-actual statistics.
public record ClassCreateDto(string Name, Guid AcademicYearId, int Grade, int? Capacity);
