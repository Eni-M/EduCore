using EduCore.Data;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

// Reads and writes transcript rows.
//
// Every query here uses IgnoreQueryFilters, and that is the entire point of the class rather
// than an optimisation: a transcript is assembled from years the student spent at schools the
// caller has no claim on, so the school filter would return an empty history for exactly the
// student it was asked about. Access is decided before any of this runs, by
// IAccessGuard.EnsureCanViewStudentAsync -- the caller must be able to see the student today,
// and having established that, they see the whole record.
public class StudentAcademicRecordRepository(AppDbContext context) : IStudentAcademicRecordRepository
{
    public async Task<string?> GetStudentNameAsync(Guid studentId, CancellationToken ct) =>
        await context.Students
            .IgnoreQueryFilters()
            .Where(s => s.Id == studentId)
            .Select(s => (s.User.FirstName + " " + s.User.LastName).Trim())
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<StudentAcademicRecord>> GetForStudentAsync(
        Guid studentId, CancellationToken ct) =>
        await context.StudentAcademicRecords
            .IgnoreQueryFilters()
            .Include(r => r.Subjects)
            .Where(r => r.StudentId == studentId)
            // Newest year first; SchoolName breaks ties for a student who moved mid-year and so
            // has two records with the same start date.
            .OrderByDescending(r => r.EnrolledAt)
            .ThenBy(r => r.SchoolName)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<EnrollmentYearRow>> GetEnrollmentYearsAsync(
        Guid studentId, CancellationToken ct) =>
        await (from e in context.Enrollments.IgnoreQueryFilters()
               join c in context.Classes.IgnoreQueryFilters() on e.ClassId equals c.Id
               join y in context.AcademicYears.IgnoreQueryFilters() on c.AcademicYearId equals y.Id
               join s in context.Schools.IgnoreQueryFilters() on e.SchoolId equals s.Id
               where e.StudentId == studentId
               orderby e.EnrolledAt descending
               select new EnrollmentYearRow(
                   e.Id,
                   e.SchoolId,
                   s.Name,
                   y.Id,
                   y.Name,
                   c.Grade,
                   c.Name,
                   e.EnrolledAt,
                   e.LeftAt,
                   e.ExitReason))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SubjectPeriodMarkRow>> GetPeriodMarksAsync(
        Guid enrollmentId, CancellationToken ct) =>
        await (from m in context.Marks.IgnoreQueryFilters()
               join sub in context.Subjects.IgnoreQueryFilters() on m.SubjectId equals sub.Id
               where m.EnrollmentId == enrollmentId
               select new SubjectPeriodMarkRow(sub.Id, sub.Name, m.FirstMark, m.SecondMark))
            .ToListAsync(ct);

    public void Add(StudentAcademicRecord record) => context.StudentAcademicRecords.Add(record);

    public void Remove(StudentAcademicRecord record) => context.StudentAcademicRecords.Remove(record);

    public async Task<bool> SaveChangesAsync(CancellationToken ct) =>
        await context.SaveChangesAsync(ct) > 0;
}
