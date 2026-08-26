namespace EduCore.Dtos.Statistics.Aggregate;

// "Class A vs Class B, Mathematics, 4.1 vs 3.6" -- one row per class for a single subject.
public record SubjectComparisonDto(
    Guid SubjectId,
    string SubjectName,
    IReadOnlyList<AcademicByKeyDto> Classes);
