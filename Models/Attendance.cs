using EduCore.Data;

namespace EduCore.Models;

public class Attendance : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid DiaryEntryId { get; set; }
    public Guid EnrollmentId { get; set; }

    public Guid ClassId { get; set; }
    public Guid SchoolId { get; set; }

    public bool IsPresent { get; private set; } = true;
    public bool IsExcused { get; private set; }

    public string? Comment { get; set; }
    public int? BehaviorMark { get; set; }

    public void Record(bool isPresent, bool isExcused)
    {
        IsPresent = isPresent;
        IsExcused = !isPresent && isExcused;
    }
}
