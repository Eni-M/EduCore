namespace EduCore.Dtos.Statistics.Aggregate;

public record GradeDistributionDto(int Mark, int Count, double Percentage);

public record GradeDistributionResultDto(
    int TotalMarks,
    bool Suppressed,
    IReadOnlyList<GradeDistributionDto> Buckets);
