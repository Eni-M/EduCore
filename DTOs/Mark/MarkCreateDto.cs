using EduCore.Enums;

namespace EduCore.Dtos.Mark;

// ClassId/AcademicYearId/SchoolId/ClassSubjectTeacherId/TeacherId are all derived server-side
// from (EnrollmentId's class, SubjectId) -- resolving the active ClassSubjectTeacher
// assignment for that pairing. Mark now requires the officially assigned teacher (unlike
// DiaryEntry), so there's no separate TeacherId to pass here: whichever teacher is currently
// assigned is who gets recorded.
public record MarkCreateDto(
    Guid EnrollmentId,
    Guid SubjectId,
    Guid GradingPeriodId,
    MarkType Type,
    int Value,
    DateOnly Date);
