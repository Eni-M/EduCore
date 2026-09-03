namespace EduCore.Models;

// Where a student's family comes from. Deliberately separate from the school
// geography (State/City/Zone): a student may come from outside Kosovo, but a
// school is always placed inside it.
public class Country
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;

    public ICollection<OriginCity> Cities { get; set; } = new List<OriginCity>();
}
