using EduCore.Data;
using EduCore.Enums;

namespace EduCore.Models;

public class Mark : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid EnrollmentId { get; set; }

    // Denormalized so the DB can enforce (via composite FKs) that this mark's class matches
    // the enrollment it's tied to, and that its grading period belongs to the same academic
    // year as the class.
    public Guid ClassId { get; set; }
    public Guid AcademicYearId { get; set; }
    public Guid SchoolId { get; set; }

    // Points at the specific ClassSubjectTeacher row active when the mark was given -- unlike
    // DiaryEntry, Mark requires the teacher to be the one actually assigned to the class,
    // enforced by a composite FK matching every denormalized field below against that row.
    public Guid ClassSubjectTeacherId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid TeacherId { get; set; }

    public Guid GradingPeriodId { get; set; }
    public MarkType Type { get; set; }
    public int Value { get; set; }
    public DateOnly Date { get; set; }
}
