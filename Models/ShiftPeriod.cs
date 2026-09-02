using EduCore.Data;

namespace EduCore.Models;

// One numbered lesson slot within a shift, e.g. Period 1 08:00-08:45.
public class ShiftPeriod : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public Guid SchoolShiftId { get; set; }
    public int PeriodNumber { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public SchoolShift Shift { get; set; } = null!;
}
