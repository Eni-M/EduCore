using EduCore.Enums;

namespace EduCore.Models;

// One year of a student's schooling at one school, kept so it survives them leaving.
//
// Deliberately NOT ISchoolScoped: scoping filters to the caller's schools, which is exactly wrong
// for a transcript, where the rows worth reading belong to a school the student has left. Access
// is decided in StudentAcademicHistoryService instead.
//
// School, year and subject names are copied in rather than referenced, so a transcript still
// reads correctly after a rename or a deletion. Marks are stored, verdicts are not -- pass, fail
// and averages derive from the current grading rules on the way out.
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

// One subject within a record, carrying the annual mark the student finished it with.
public class StudentAcademicRecordSubject
{
    public Guid Id { get; set; }

    public Guid StudentAcademicRecordId { get; set; }
    public StudentAcademicRecord Record { get; set; } = null!;

    // Null if the subject has since been removed from the school's catalogue.
    public Guid? SubjectId { get; set; }
    public string SubjectName { get; set; } = null!;

    // Averaged and rounded as IGradingCalculator.AnnualMark does. Null when never graded.
    public int? AnnualMark { get; set; }
}
