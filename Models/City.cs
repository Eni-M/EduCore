namespace EduCore.Models;

public class City
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public Guid StateId { get; set; }

    public State State { get; set; } = null!;
}
