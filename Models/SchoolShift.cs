using EduCore.Data;

namespace EduCore.Models;

// A named block of the school day (e.g. "Morning") that owns an ordered set of lesson periods.
public class SchoolShift : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; } = true;

    public ICollection<ShiftPeriod> Periods { get; set; } = new List<ShiftPeriod>();
}
