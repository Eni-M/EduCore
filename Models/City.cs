namespace EduCore.Models;

// A city always belongs to a state; there is no such thing as an unplaced city. Students and
// schools point here rather than storing a name, so renaming a city corrects every record
// that references it at once.
public class City
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public Guid StateId { get; set; }

    public State State { get; set; } = null!;
}
