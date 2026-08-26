using EduCore.Enums;

namespace EduCore.Models;

public class School
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public SchoolType Type { get; set; }

    // Where the school actually is. Nullable because schools created before this existed have
    // no recorded location, and guessing one would put invented places in a demographic
    // report. Zone is the sub-area within a city (district, municipality, village).
    public string? City { get; set; }
    public string? Zone { get; set; }
}
