using EduCore.Data;

namespace EduCore.Models;

// A recurring planned lesson: this class takes this subject with this teacher every week
// on DayOfWeek at ShiftPeriod. Planning only -- what actually happened lives in DiaryEntry.
public class ScheduleEntry : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public Guid AcademicYearId { get; set; }
    public Guid ClassId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid TeacherId { get; set; }
    public Guid ShiftPeriodId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
}
