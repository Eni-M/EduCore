namespace EduCore.Dtos.Statistics.Aggregate;

// GenderNotRecorded counts students created before gender was captured. Reported rather than
// folded into either group, so the gap is visible instead of silently distorting the split.
public record StudentStatisticsDto(
    int Total,
    int Active,
    int Graduated,
    int NewArrivals,
    int Female,
    int Male,
    int GenderNotRecorded);
