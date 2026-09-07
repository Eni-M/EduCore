using EduCore.Enums;

namespace EduCore.Models;

/// <summary>
/// One year of a student's schooling, at one school, kept so it survives the student leaving
/// that school.
///
/// Deliberately NOT <c>ISchoolScoped</c>. Every scoped entity is filtered to
/// <c>currentUser.SchoolIds</c>, which is exactly wrong for a transcript: the rows worth reading
/// are the ones belonging to the school the student has already left. A receiving school cannot
/// see a student's previous year through Enrollments or Marks for that reason, even though the
/// data is sitting in both. This table is the answer to that, so access is decided in the
/// service layer instead — see StudentAcademicHistoryService.
///
/// School and year names are copied in, not just referenced. A transcript has to still read
/// correctly years later, after a school is renamed, merged into another, or removed; a record
/// that renders through today's foreign keys quietly rewrites history when the present changes.
/// This is one of the few places where duplicating data is the correct choice.
///
/// What is stored is the mark, never the verdict. Pass, fail and averages are derived on the way
/// out from the current grading configuration, exactly as SchoolStatisticsSnapshot stores raw
/// aggregates rather than presented figures -- freezing "passed" into the row would make every
/// historical record disagree with the rules the moment a threshold is adjusted.
/// </summary>
public class StudentAcademicRecord
{
    public Guid Id { get; set; }

    public Guid StudentId { get; set; }

    // Kept for joining back to a school that still exists; SchoolName is what gets displayed.
    public Guid SchoolId { get; set; }
    public string SchoolName { get; set; } = null!;

    // Null once the originating academic year is deleted. AcademicYearName still renders.
    public Guid? AcademicYearId { get; set; }
    public string AcademicYearName { get; set; } = null!;

    public int Grade { get; set; }
    public string? ClassName { get; set; }

    public DateOnly EnrolledAt { get; set; }
    public DateOnly? LeftAt { get; set; }
    public EnrollmentExitReason? ExitReason { get; set; }

    // When this row was last rebuilt from the live tables. A record for a year still in progress
    // is expected to change; one for a closed year should not.
    public DateTime CompiledAt { get; set; }

    public ICollection<StudentAcademicRecordSubject> Subjects { get; set; } = [];
}

/// <summary>
/// One subject within a <see cref="StudentAcademicRecord"/>, carrying the annual mark the
/// student finished that subject with. Subject names are copied in for the same reason school
/// names are.
/// </summary>
public class StudentAcademicRecordSubject
{
    public Guid Id { get; set; }

    public Guid StudentAcademicRecordId { get; set; }
    public StudentAcademicRecord Record { get; set; } = null!;

    // Null if the subject has since been removed from the school's catalogue.
    public Guid? SubjectId { get; set; }
    public string SubjectName { get; set; } = null!;

    // The average of the period final marks, rounded the same way IGradingCalculator.AnnualMark
    // rounds it. Null when the student was never graded in this subject that year.
    public int? AnnualMark { get; set; }
}
