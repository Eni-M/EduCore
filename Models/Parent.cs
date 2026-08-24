namespace EduCore.Models;

public class Parent
{
    public Guid Id { get; private set; }
    public Guid StudentId { get; private set; }
    public string Name { get; private set; }
    public string PhoneNumber { get; private set; }

    public Parent(Guid studentId, string name, string phoneNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);

        Id = Guid.NewGuid();
        StudentId = studentId;
        Name = name;
        PhoneNumber = phoneNumber;
    }
}
