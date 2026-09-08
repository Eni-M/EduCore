using EduCore.Data;
using EduCore.Dtos.Assignment;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class AssignmentRepository(AppDbContext context)
    : Repository<Assignment>(context), IAssignmentRepository
{
    private IQueryable<AssignmentListDto> Project(
        IQueryable<Assignment> assignments) =>
        from a in assignments
        join c in _context.Classes on a.ClassId equals c.Id
        join s in _context.Subjects on a.SubjectId equals s.Id
        join t in _context.Teachers on a.TeacherId equals t.Id
        join u in _context.Users on t.UserId equals u.Id
        select new AssignmentListDto(
            a.Id,
            a.ClassId,
            c.Name,
            a.SubjectId,
            s.Name,
            a.TeacherId,
            u.FirstName + " " + u.LastName,
            a.Title,
            a.DueDate,
            a.StorageKey != null,
            a.CreatedAt);

    public async Task<IEnumerable<AssignmentListDto>> GetByClassAsync(
        Guid classId,
        CancellationToken ct)
    {
        var results = await Project(
                _context.Assignments
                    .AsNoTracking()
                    .Where(a => a.ClassId == classId))
            .ToListAsync(ct);

        return results.OrderBy(a => a.DueDate);
    }

    public async Task<IEnumerable<AssignmentListDto>> GetByTeacherAsync(
        Guid teacherId,
        CancellationToken ct)
    {
        var results = await Project(
                _context.Assignments
                    .AsNoTracking()
                    .Where(a => a.TeacherId == teacherId))
            .ToListAsync(ct);

        return results
            .OrderByDescending(a => a.CreatedAt);
    }

    public async Task<AssignmentDetailsDto?> GetDetailsAsync(
        Guid assignmentId,
        CancellationToken ct)
    {
        return await (
            from a in _context.Assignments.AsNoTracking()
            join c in _context.Classes on a.ClassId equals c.Id
            join s in _context.Subjects on a.SubjectId equals s.Id
            join t in _context.Teachers on a.TeacherId equals t.Id
            join u in _context.Users on t.UserId equals u.Id
            where a.Id == assignmentId
            select new AssignmentDetailsDto(
                a.Id,
                a.ClassId,
                c.Name,
                a.SubjectId,
                s.Name,
                a.TeacherId,
                u.FirstName + " " + u.LastName,
                a.Title,
                a.Description,
                a.DueDate,
                a.FileName,
                a.ContentType,
                a.SizeBytes,
                a.CreatedAt,
                a.UpdatedAt))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IEnumerable<AssignmentCalendarDto>> GetForCalendarAsync(
         Guid[] classIds,
         DateOnly? from,
         DateOnly? to,
         CancellationToken ct)
    {
        if (classIds.Length == 0)
            return [];

        var query = _context.Assignments
            .AsNoTracking()
            .Where(a => classIds.Contains(a.ClassId));

        if (from is not null)
        {
            // Convert DateOnly to a UTC DateTimeOffset
            var fromDto = new DateTimeOffset(from.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            query = query.Where(a => a.DueDate >= fromDto);
        }

        if (to is not null)
        {
            // Convert DateOnly to a UTC DateTimeOffset
            var toDto = new DateTimeOffset(to.Value.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
            query = query.Where(a => a.DueDate <= toDto);
        }

        return await (
            from a in query
            join c in _context.Classes on a.ClassId equals c.Id
            join t in _context.Teachers on a.TeacherId equals t.Id
            join u in _context.Users on t.UserId equals u.Id
            select new AssignmentCalendarDto(
                a.Id,
                a.Title,
                a.Description,
                a.ClassId,
                c.Name,
                a.DueDate,
                a.TeacherId,
                u.FirstName + " " + u.LastName))
            .ToListAsync(ct);
    }
}