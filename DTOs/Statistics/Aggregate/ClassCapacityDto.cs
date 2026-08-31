namespace EduCore.Dtos.Statistics.Aggregate;

public record ClassCapacityDto(
    Guid ClassId,
    string ClassName,
    int Grade,
    int? Capacity,
    int Enrolled,
    Measure UtilisationPercent);
