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

    // FirstMark and SecondMark weighted under the Grading weights in force when the mark was
    // written; null until both exist. Every final, pass rate and statistic reads this, so a later
    // change to the weights applies to new marks only and never rewrites recorded ones.
    public double? WeightedValue { get; set; }

    // When this mark was last written. Null for every row predating the column, which is the
    // honest answer -- nobody recorded when those were recorded. Digests ask "what changed this
    // week", so an unknown date correctly excludes a row rather than inventing one for it.
    public DateTime? RecordedAt { get; set; }
}
