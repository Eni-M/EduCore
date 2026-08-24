using EduCore.Data;

namespace EduCore.Models;

public class GradingPeriod : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid AcademicYearId { get; set; }
    public Guid SchoolId { get; set; }
    public string Name { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}
