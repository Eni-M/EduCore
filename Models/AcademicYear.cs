using EduCore.Data;

namespace EduCore.Models;

// School-scoped, not global -- term/year boundaries genuinely differ between schools.
public class AcademicYear : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public string Name { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}
