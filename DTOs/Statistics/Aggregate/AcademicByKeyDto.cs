namespace EduCore.Dtos.Statistics.Aggregate;

public record AcademicByKeyDto(
    string Key,
    string Label,
    int GradedMarks,
    Measure AverageFinalMark,
    Measure PassRate);
