using EduCore.Dtos.Statistics;
using EduCore.Dtos.Statistics.Aggregate;
using EduCore.Models;
using EduCore.Services.Statistics.Interfaces;

namespace EduCore.Services.Statistics;

/// <summary>
/// Rebuilds the summary DTOs from a stored snapshot.
///
/// The snapshot holds raw counts and sums only. Every rate, average and suppression decision is
/// made here, exactly as the live path makes it, so that changing the privacy threshold or a
/// rounding rule takes effect on the next request rather than requiring every stored row to be
/// recomputed. It also means a snapshot-served response and a live-computed one are the same
/// bytes, which is what makes the two paths safely interchangeable.
/// </summary>
public static class SnapshotProjection
{
    public static StudentStatisticsDto Students(SchoolStatisticsSnapshot s) =>
        new(s.StudentsTotal,
            s.StudentsActive,
            s.StudentsGraduated,
            s.StudentsNewArrivals,
            s.StudentsFemale,
            s.StudentsMale,
            s.StudentsGenderNotRecorded);

    public static EnrollmentStatisticsDto Enrollment(SchoolStatisticsSnapshot s) =>
        new(s.EnrollmentsActive,
            s.EnrollmentsStarted,
            s.EnrollmentsExited,
            Measures.GrowthPercent(s.EnrollmentsActive, s.EnrollmentsActiveYearAgo));

    public static AttendanceStatisticsDto Attendance(
        SchoolStatisticsSnapshot s, ICellSuppressor suppressor) =>
        new(s.AttendanceRecords,
            suppressor.Apply(Measures.Rate(s.AttendancePresent, s.AttendanceRecords)),
            suppressor.Apply(Measures.Rate(s.AttendanceAbsent, s.AttendanceRecords)),
            suppressor.Apply(Measures.Rate(s.AttendanceExcusedAbsences, s.AttendanceRecords)),
            suppressor.Apply(Measures.Average(
                s.AttendanceAverageBehaviorMark, s.AttendanceBehaviorMarkCount)));

    public static AcademicStatisticsDto Academic(
        SchoolStatisticsSnapshot s, ICellSuppressor suppressor) =>
        new(s.AcademicGradedMarks,
            s.AcademicIncompleteMarks,
            suppressor.Apply(Measures.Average(s.AcademicAverageFinalMark, s.AcademicGradedMarks)),
            suppressor.Apply(Measures.Rate(s.AcademicPassed, s.AcademicGradedMarks)),
            suppressor.Apply(Measures.Rate(s.AcademicFailed, s.AcademicGradedMarks)),
            suppressor.Apply(Measures.Rate(s.AcademicAtRisk, s.AcademicGradedMarks)));

    public static TeacherStatisticsDto Teachers(
        SchoolStatisticsSnapshot s, ICellSuppressor suppressor) =>
        new(s.Teachers,
            s.Teachers == 0
                ? Measure.None()
                : suppressor.Apply(Measures.Average((double)s.StudentsActive / s.Teachers, s.Teachers)),
            s.TeachersWithWorkload == 0
                ? Measure.None()
                : suppressor.Apply(Measures.Average(
                    (double)s.WorkloadClassesSum / s.TeachersWithWorkload, s.TeachersWithWorkload)));
}
