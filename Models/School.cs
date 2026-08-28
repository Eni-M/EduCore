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

    // The replacement for the free-text City above. Both exist for now: the demographics
    // queries still group on the strings, and switching them over is its own change.
    public Guid? CityId { get; set; }

    // A timestamp rather than a bool, for the same reason User.DeactivatedAt is one: an
    // administrator looking at a switched-off school wants to know since when, and the bool
    // is one comparison away. Null means the school is running.
    //
    // Switching a school off hides it from the pickers that create new records against it.
    // It deliberately does NOT lock anyone out: the school keeps its history, its people keep
    // working, and nothing is deleted -- the flag is reversible in one click.
    public DateTime? DeactivatedAt { get; set; }
}
