namespace EduCore.Dtos.Student;

// Graduation is a deliberate, admin-supplied-date action, not a plain field edit.
public record StudentGraduateDto(DateOnly GraduationDate);
