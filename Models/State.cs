namespace EduCore.Models;

public class State
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;

    public ICollection<City> Cities { get; set; } = new List<City>();
}
