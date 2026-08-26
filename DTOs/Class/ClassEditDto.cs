namespace EduCore.Dtos.Class;

// SchoolId/AcademicYearId are intentionally not editable here -- moving a class to another
// school or year isn't a plain field edit, it's a separate operation if ever needed at all.
public record ClassEditDto(string Name, int Grade, int? Capacity);
