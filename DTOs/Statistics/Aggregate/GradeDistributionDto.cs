namespace EduCore.Dtos.Statistics.Aggregate;

// How many final marks landed on each point of the 1-5 scale.
public record GradeDistributionDto(int Mark, int Count, double Percentage);

// A distribution is more disclosive than the average sitting beside it: with one graded mark,
// the buckets spell out that pupil's exact grade. So the whole distribution is withheld when
// it was computed from too few marks, rather than each bucket being suppressed individually --
// a partly-suppressed distribution still gives the answer away by subtraction.
public record GradeDistributionResultDto(
    int TotalMarks,
    bool Suppressed,
    IReadOnlyList<GradeDistributionDto> Buckets);
