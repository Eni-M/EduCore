using EduCore.Data;
using EduCore.Dtos.Assignment;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class AssignmentSubmissionRepository(AppDbContext context)
    : Repository<AssignmentSubmission>(context), IAssignmentSubmissionRepository
{
    // Status is derived, never stored: Missing means no row exists for that student at all,
    // so it can only be computed by starting from Enrollments and left-joining submissions.
    private async Task<IEnumerable<AssignmentSubmissionDto>> ProjectAsync(
        Guid assignmentId,
        Guid? onlyStudentId,
        CancellationToken ct)
    {
        var assignment = await _context.Assignments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == assignmentId, ct);

        if (assignment is null)
            return [];

        var enrollments =
            from e in _context.Enrollments.AsNoTracking()
            where e.ClassId == assignment.ClassId && e.LeftAt == null
            select e;

        if (onlyStudentId is not null)
            enrollments = enrollments.Where(e => e.StudentId == onlyStudentId);

        var rows = await (
            from e in enrollments
            join st in _context.Students on e.StudentId equals st.Id
            join u in _context.Users on st.UserId equals u.Id
            join sub in _context.AssignmentSubmissions.Where(s => s.AssignmentId == assignmentId)
                on e.StudentId equals sub.StudentId into subs
            from sub in subs.DefaultIfEmpty()
            select new
            {
                e.StudentId,
                StudentName = u.FirstName + " " + u.LastName,
                Submission = sub
            })
            .ToListAsync(ct);

        return rows
            .Select(r =>
            {
                var status = r.Submission is null
                    ? "Missing"
                    : r.Submission.SubmittedAt > assignment.DueDate
                        ? "Late"
                        : "Submitted";

                return new AssignmentSubmissionDto(
                    r.Submission?.Id,
                    assignmentId,
                    r.StudentId,
                    r.StudentName,
                    status,
                    r.Submission?.SubmittedAt,
                    r.Submission?.FileName,
                    r.Submission?.ContentType,
                    r.Submission?.SizeBytes,
                    r.Submission?.Score,
                    r.Submission?.Feedback);
            })
            .OrderBy(r => r.StudentName)
            .ToList();
    }

    public Task<AssignmentSubmission?> GetByAssignmentAndStudentAsync(
        Guid assignmentId,
        Guid studentId,
        CancellationToken ct) =>
        _context.AssignmentSubmissions
            .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.StudentId == studentId, ct);

    public async Task<AssignmentSubmissionDto?> GetForStudentAsync(
        Guid assignmentId,
        Guid studentId,
        CancellationToken ct) =>
        (await ProjectAsync(assignmentId, studentId, ct)).FirstOrDefault();

    public async Task<IEnumerable<AssignmentSubmissionDto>> GetForAssignmentAsync(
        Guid assignmentId,
        CancellationToken ct) =>
        await ProjectAsync(assignmentId, null, ct);
}