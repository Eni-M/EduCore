namespace EduCore.Dtos.Statistics.Aggregate;

public record SubjectComparisonDto(
    Guid SubjectId,
    string SubjectName,
    IReadOnlyList<AcademicByKeyDto> Classes);
