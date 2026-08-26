namespace EduCore.Dtos.Statistics.Aggregate;

// How many final marks landed on each point of the 1-5 scale.
public record GradeDistributionDto(int Mark, int Count, double Percentage);
