using EduCore.Data;

namespace EduCore.Models;

public class Admin : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid SchoolId { get; set; }

    public User User { get; set; } = null!;
    public School School { get; set; } = null!;
}
