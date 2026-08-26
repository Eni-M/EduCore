namespace EduCore.Dtos.Statistics.Rows;

public record AcademicGroup(
    string Key,
    string Label,
    int GradedMarks,
    double? AverageFinalMark,
    int Passed);
