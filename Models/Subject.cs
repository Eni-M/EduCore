using EduCore.Data;

namespace EduCore.Models;

public class Subject : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public string Name { get; set; } = null!;
}
