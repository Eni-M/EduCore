namespace EduCore.Dtos.Mark;

// Create-or-update: there is exactly one mark per (enrollment, subject, grading period), and
// a teacher typically sets FirstMark mid-period and SecondMark at the end. Either may be null
// while the period is still running.
//
// ClassId/AcademicYearId/SchoolId/ClassSubjectTeacherId/TeacherId are all derived server-side
// from (EnrollmentId's class, SubjectId) -- resolving the active ClassSubjectTeacher
// assignment for that pairing. Mark requires the officially assigned teacher (unlike
// DiaryEntry), so whichever teacher is currently assigned is who gets recorded.
public record MarkUpsertDto(
    Guid EnrollmentId,
    Guid SubjectId,
    Guid GradingPeriodId,
    int? FirstMark,
    int? SecondMark);
