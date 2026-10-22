namespace EduCore.Dtos.Mark;

public record MarkListDto(
    Guid Id,
    Guid EnrollmentId,
    string StudentName,
    string ClassName,
    Guid SubjectId,
    string SubjectName,
    Guid TeacherId,
    string TeacherName,
    Guid GradingPeriodId,
    string GradingPeriodName,
    int? FirstMark,
    int? SecondMark,
    int? FinalMark,
    [property: System.Text.Json.Serialization.JsonIgnore] double? WeightedValue = null,
    uint Version = 0,
    bool PeriodClosed = false);
