using EduCore.Data;
using EduCore.Dtos.Enrollment;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class EnrollmentRepository(AppDbContext context)
    : Repository<Enrollment>(context), IEnrollmentRepository
{
    // Ordering has to happen either on the source query or in memory afterwards. EF cannot
    // sort on a property of an already-projected record, because it has no way to map that
    // property back onto a constructor argument.
    private IQueryable<EnrollmentListDto> Project(IQueryable<Enrollment> enrollments) =>
        from e in enrollments
        join s in _context.Students on e.StudentId equals s.Id
        join u in _context.Users on s.UserId equals u.Id
        join c in _context.Classes on e.ClassId equals c.Id
        select new EnrollmentListDto(
            e.Id,
            e.StudentId,
            u.FirstName + " " + u.LastName,
            e.ClassId,
            c.Name,
            e.EnrolledAt,
            e.LeftAt,
            e.ExitReason);

    public async Task<EnrollmentListDto?> GetDtoAsync(Guid enrollmentId, CancellationToken ct) =>
        await Project(_context.Enrollments.AsNoTracking().Where(e => e.Id == enrollmentId))
            .FirstOrDefaultAsync(ct);

    // Sorted in memory: the name lives on the joined user, and a class roster is small.
    public async Task<IEnumerable<EnrollmentListDto>> GetByClassAsync(
        Guid classId,
        bool includeLeft,
        CancellationToken ct)
    {
        var enrollments = await Project(_context.Enrollments
                .AsNoTracking()
                .Where(e => e.ClassId == classId && (includeLeft || e.LeftAt == null)))
            .ToListAsync(ct);

        return enrollments.OrderBy(e => e.StudentName);
    }

    public async Task<IEnumerable<EnrollmentListDto>> GetByStudentAsync(Guid studentId, CancellationToken ct) =>
        await Project(_context.Enrollments
                .AsNoTracking()
                .Where(e => e.StudentId == studentId)
                .OrderByDescending(e => e.EnrolledAt))
            .ToListAsync(ct);

    public async Task<IEnumerable<Enrollment>> GetActiveByClassAsync(Guid classId, CancellationToken ct) =>
        await _context.Enrollments
            .Where(e => e.ClassId == classId && e.LeftAt == null)
            .ToListAsync(ct);

    public async Task<Enrollment?> GetActiveForStudentAsync(Guid studentId, CancellationToken ct) =>
        await _context.Enrollments
            .Where(e => e.StudentId == studentId && e.LeftAt == null)
            .OrderByDescending(e => e.EnrolledAt)
            .FirstOrDefaultAsync(ct);
}
