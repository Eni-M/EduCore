namespace EduCore.Models;

public class Admin
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid SchoolId { get; private set; }

    public Admin(Guid userId, Guid schoolId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        SchoolId = schoolId;
    }
}
