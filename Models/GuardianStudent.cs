using EduCore.Data;

namespace EduCore.Models;

public class GuardianStudent : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid GuardianId { get; set; }
    public Guid StudentId { get; set; }
    public Guid SchoolId { get; set; }

    public Guardian Guardian { get; set; } = null!;
    public Student Student { get; set; } = null!;
}
