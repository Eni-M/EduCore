using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface IStudentAcademicRecordRepository
{
    // The student's display name. A dedicated one-column query: the only existing way to load
    // a Student with its User attached also pulls Country, OriginCity and the whole guardian
    // graph, which is a lot of joins to render a heading.
    Task<string?> GetStudentNameAsync(Guid studentId, CancellationToken ct);

    // Every year on record for one student, across all schools, with subject lines loaded.
    Task<IReadOnlyList<StudentAcademicRecord>> GetForStudentAsync(Guid studentId, CancellationToken ct);

    // The live enrollments a record set is compiled from. Cross-school by necessity.
    Task<IReadOnlyList<EnrollmentYearRow>> GetEnrollmentYearsAsync(Guid studentId, CancellationToken ct);

    // Period final marks for one enrollment, one row per subject per grading period.
    Task<IReadOnlyList<SubjectPeriodMarkRow>> GetPeriodMarksAsync(Guid enrollmentId, CancellationToken ct);

    void Add(StudentAcademicRecord record);
    void Remove(StudentAcademicRecord record);
    Task<bool> SaveChangesAsync(CancellationToken ct);
}

// Flat projections rather than entity graphs: compiling history reads across schools, so these
// deliberately carry only the columns the compiler needs.
public record EnrollmentYearRow(
    Guid EnrollmentId,
    Guid SchoolId,
    string SchoolName,
    Guid? AcademicYearId,
    string AcademicYearName,
    int Grade,
    string? ClassName,
    DateOnly EnrolledAt,
    DateOnly? LeftAt,
    Enums.EnrollmentExitReason? ExitReason);

public record SubjectPeriodMarkRow(
    Guid? SubjectId,
    string SubjectName,
    int? FirstMark,
    int? SecondMark,
    double? WeightedValue);
