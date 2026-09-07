namespace EduCore.Dtos.Statistics.Rows;

// Raw capacity figures for one class. A named record rather than a tuple so the query can
// project into it directly and be paged in the database.
public record ClassCapacityRow(
    Guid ClassId,
    string ClassName,
    int Grade,
    int? Capacity,
    int Enrolled);
