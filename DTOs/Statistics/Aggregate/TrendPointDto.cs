namespace EduCore.Dtos.Statistics.Aggregate;

// One point on a time series. Label is pre-formatted for the axis ("Sep 2026", "Semester 1")
// so every chart in the frontend does not have to reinvent the same formatting.
public record TrendPointDto(DateOnly From, DateOnly To, string Label, Measure Value);
