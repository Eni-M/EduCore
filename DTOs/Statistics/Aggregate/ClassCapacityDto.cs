namespace EduCore.Dtos.Statistics.Aggregate;

// Capacity is null for classes that never declared one; Utilisation is then empty rather than
// zero or infinite.
public record ClassCapacityDto(
    Guid ClassId,
    string ClassName,
    int Grade,
    int? Capacity,
    int Enrolled,
    Measure UtilisationPercent);
