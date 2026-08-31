namespace EduCore.Dtos.Statistics.Aggregate;

public record StudentStatisticsDto(
    int Total,
    int Active,
    int Graduated,
    int NewArrivals,
    int Female,
    int Male,
    int GenderNotRecorded);
