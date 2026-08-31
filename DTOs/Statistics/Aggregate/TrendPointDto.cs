namespace EduCore.Dtos.Statistics.Aggregate;

public record TrendPointDto(DateOnly From, DateOnly To, string Label, Measure Value);
