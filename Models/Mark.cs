using EduCore.Data;

namespace EduCore.Models;

public class Mark : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid EnrollmentId { get; set; }

    public Guid ClassId { get; set; }
    public Guid AcademicYearId { get; set; }
    public Guid SchoolId { get; set; }

    public Guid ClassSubjectTeacherId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid TeacherId { get; set; }

    public Guid GradingPeriodId { get; set; }

    public int? FirstMark { get; set; }
    public int? SecondMark { get; set; }
}
