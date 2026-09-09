using EduCore.Data;

namespace EduCore.Models;

// Pre-computed statistics for one school over one reporting window, so the overview screens
// read rows instead of fanning out across six tables per request.
//
// Raw aggregates only, never presented figures: rates, averages and small-cell suppression are
// derived on the way out, so a change to the privacy threshold takes effect immediately. Only
// unfiltered single-school queries are snapshotted; anything narrower computes live.
public class SchoolStatisticsSnapshot : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }

    // --- Key: which question these numbers answer -------------------------------------
    // Both ids also filter marks directly, so they are part of the row's identity.
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public Guid? AcademicYearId { get; set; }
    public Guid? GradingPeriodId { get; set; }

    // --- Freshness ---------------------------------------------------------------------
    public DateTime ComputedAt { get; set; }

    // Counted since ComputedAt by StatisticsInvalidationInterceptor; see SnapshotStaleness.
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

    // Sum and count rather than the average itself, which would round twice.
    public int TeachersWithWorkload { get; set; }
    public int WorkloadClassesSum { get; set; }
}
