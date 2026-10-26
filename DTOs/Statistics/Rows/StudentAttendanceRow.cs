namespace EduCore.Dtos.Statistics.Rows;

// Init properties, not a positional constructor: the paged queries filter and order on these
// members after the GroupBy, and EF can only see through a member-initialised projection.
public record StudentAttendanceRow
{
    public Guid StudentId { get; init; }
    public string Name { get; init; } = "";
    public Guid ClassId { get; init; }
    public string ClassName { get; init; } = "";
    public int Records { get; init; }
    public int Present { get; init; }
    public int Excused { get; init; }
}
