using EduCore.Data;

namespace EduCore.Models;

/// <summary>
/// Pre-computed statistics for one school over one reporting window.
///
/// The overview screens used to derive every figure by fanning out across Students,
/// Enrollments, Attendances, Marks, Classes and Teachers on each request — around seven
/// statements for the student block alone, repeated per school. The platform overview did
/// that for all 80 schools in a loop. This table holds the answers instead, so those screens
/// read rows rather than recomputing aggregates.
///
/// What is stored is the raw aggregate (counts and sums), never the presented figure. Rates,
/// averages and small-cell suppression are still derived in the service layer on the way out,
/// so a change to the privacy threshold takes effect immediately instead of being frozen into
/// every stored row.
///
/// Only unfiltered, single-school queries are snapshotted. Narrowing by class, grade, subject
/// or teacher multiplies the key space far beyond what is worth storing, and those queries
/// fall through to live computation.
/// </summary>
public class SchoolStatisticsSnapshot : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }

    // --- Key: which question these numbers answer -------------------------------------
    // From/To already encode the academic year or grading period the window came from, but
    // both ids also filter marks directly, so they are part of the identity of the row.
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public Guid? AcademicYearId { get; set; }
    public Guid? GradingPeriodId { get; set; }

    // --- Freshness ---------------------------------------------------------------------
    public DateTime ComputedAt { get; set; }

    // Writes to the tables these figures are drawn from, counted since ComputedAt by
    // StatisticsInvalidationInterceptor. Compared against a school-size-dependent threshold
    // to decide when the numbers are worth computing again.
    public int ChangesSinceComputed { get; set; }

    // --- Students -----------------------------------------------------------------------
    public int StudentsTotal { get; set; }
    public int StudentsActive { get; set; }
    public int StudentsGraduated { get; set; }
    public int StudentsNewArrivals { get; set; }
    public int StudentsFemale { get; set; }
    public int StudentsMale { get; set; }
    public int StudentsGenderNotRecorded { get; set; }

    // --- Enrollment ---------------------------------------------------------------------
    public int EnrollmentsActive { get; set; }
    public int EnrollmentsStarted { get; set; }
    public int EnrollmentsExited { get; set; }

    // Active enrollments a year before To, the denominator of the year-on-year growth figure.
    public int EnrollmentsActiveYearAgo { get; set; }

    // --- Attendance ----------------------------------------------------------------------
    public int AttendanceRecords { get; set; }
    public int AttendancePresent { get; set; }
    public int AttendanceAbsent { get; set; }
    public int AttendanceExcusedAbsences { get; set; }
    public double? AttendanceAverageBehaviorMark { get; set; }
    public int AttendanceBehaviorMarkCount { get; set; }

    // --- Academic --------------------------------------------------------------------------
    public int AcademicGradedMarks { get; set; }
    public int AcademicIncompleteMarks { get; set; }
    public double? AcademicAverageFinalMark { get; set; }
    public int AcademicPassed { get; set; }
    public int AcademicFailed { get; set; }
    public int AcademicAtRisk { get; set; }

    // --- Teachers and classes ----------------------------------------------------------------
    public int Teachers { get; set; }
    public int Classes { get; set; }

    // Sum and count of classes taught per teacher, so "average classes per teacher" can be
    // rebuilt without storing the average itself (which would round twice).
    public int TeachersWithWorkload { get; set; }
    public int WorkloadClassesSum { get; set; }
}
