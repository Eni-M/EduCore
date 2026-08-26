using EduCore.Data;

namespace EduCore.Models;

// One row per (enrollment, subject, grading period) -- see the unique index in AppDbContext.
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

    // The two marks a teacher gives for this subject this period. What each one measures is
    // the teacher's own decision and is deliberately not recorded anywhere. Either may be null
    // while the period is still running -- the final mark stays null until both are set.
    public int? FirstMark { get; set; }
    public int? SecondMark { get; set; }
}
