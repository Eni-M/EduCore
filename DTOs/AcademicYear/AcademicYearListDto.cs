namespace EduCore.Dtos.AcademicYear;

// SchoolName is filled only for a SuperAdmin, who reads years across every school and would
// otherwise see several identically named "2025/2026" rows with no way to tell them apart.
// Null for an Admin, whose list is already their own school and for whom naming it on every
// row would be noise.
public record AcademicYearListDto(
    Guid Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    string? SchoolName);
