using EduCore.Enums;

namespace EduCore.Dtos.StudentHistory;

// A student's schooling across every year and every school, newest first. Assembled from
// StudentAcademicRecord rows, which are not school-scoped, so a receiving school sees the years
// the student spent elsewhere -- the whole reason the table exists.
public record StudentAcademicHistoryDto(
    Guid StudentId,
    string StudentName,
    IReadOnlyList<StudentAcademicYearDto> Years);

public record StudentAcademicYearDto(
    Guid SchoolId,
    string SchoolName,
    // True when this is a year at the school the caller is reading from, so the UI can separate
    // "our records" from "what the student brought with them".
    bool IsCurrentSchool,
    string AcademicYearName,
    int Grade,
    string? ClassName,
    DateOnly EnrolledAt,
    DateOnly? LeftAt,
    EnrollmentExitReason? ExitReason,
    // Derived on the way out from the marks below and the current grading rules, never stored.
    double? AverageMark,
    int SubjectsTaken,
    int SubjectsGraded,
    int SubjectsPassed,
    int SubjectsFailed,
    DateTime CompiledAt,
    IReadOnlyList<StudentAcademicSubjectDto> Subjects);

public record StudentAcademicSubjectDto(
    string SubjectName,
    int? AnnualMark,
    // Null when the subject was never graded, so the UI can tell "failed" from "no mark".
    bool? Passed);
