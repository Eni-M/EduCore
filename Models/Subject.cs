namespace EduCore.Models;

public class Subject
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }

    public Subject(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = Guid.NewGuid();
        Name = name;
    }
}
