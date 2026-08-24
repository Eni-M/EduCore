namespace EduCore.Models;

public class School
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Type { get; private set; }

    public School(string name, string type)
    {
        Id = Guid.NewGuid();
        Name = name;
        Type = type;
    }
}