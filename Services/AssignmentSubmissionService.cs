using EduCore.Dtos.Assignment;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Services.Assignments;

public class AssignmentSubmissionService(
    IAssignmentSubmissionRepository submissions,
    IAssignmentRepository assignments,
    IEnrollmentRepository enrollments,
    IAccessGuard access,
    ICurrentUser currentUser) : IAssignmentSubmissionService
{
    public async Task<AssignmentSubmissionDto> SubmitAsync(
        Guid assignmentId,
        CreateSubmissionRequest request,
        CancellationToken ct)
    {
        var studentId = currentUser.RequireStudentId();

        var assignment = await assignments.GetByIdAsync(assignmentId, ct)
                          ?? throw new NotFoundException("Assignment", assignmentId);

        var enrollment = await enrollments.GetActiveForStudentAsync(studentId, ct);

        if (enrollment?.ClassId != assignment.ClassId)
            throw new ForbiddenException("You are not enrolled in this assignment's class.");

        var existing = await submissions.GetByAssignmentAndStudentAsync(assignmentId, studentId, ct);

        if (existing is not null)
        {
            if (existing.Score is not null)
                throw new BusinessException("This assignment has already been graded and can no longer be resubmitted.");

            existing.FileName = request.FileName;
            existing.ContentType = request.ContentType;
            existing.SizeBytes = request.SizeBytes;
            existing.StorageKey = request.StorageKey;
            existing.SubmittedAt = DateTime.UtcNow;

            submissions.Update(existing);
        }
        else
        {
            await submissions.AddAsync(new AssignmentSubmission
            {
                Id = Guid.NewGuid(),
                AssignmentId = assignmentId,
                StudentId = studentId,
                SchoolId = currentUser.RequireSchoolId(),
                FileName = request.FileName,
                ContentType = request.ContentType,
                SizeBytes = request.SizeBytes,
                StorageKey = request.StorageKey,
                SubmittedAt = DateTime.UtcNow
            });
        }

        await submissions.SaveChangesAsync(ct);

        return await submissions.GetForStudentAsync(assignmentId, studentId, ct)
               ?? throw new NotFoundException("AssignmentSubmission", assignmentId);
    }

    public async Task<IEnumerable<AssignmentSubmissionDto>> GetForAssignmentAsync(
        Guid assignmentId,
        CancellationToken ct)
    {
        var assignment = await assignments.GetByIdAsync(assignmentId, ct)
                          ?? throw new NotFoundException("Assignment", assignmentId);

        await access.EnsureCanTeachClassAsync(assignment.ClassId, ct);

        return await submissions.GetForAssignmentAsync(assignmentId, ct);
    }

    public async Task<AssignmentSubmissionDto> GradeAsync(
        Guid assignmentId,
        Guid studentId,
        UpdateAssignmentSubmissionDto request,
        CancellationToken ct)
    {
        var assignment = await assignments.GetByIdAsync(assignmentId, ct)
                          ?? throw new NotFoundException("Assignment", assignmentId);

        await access.EnsureCanTeachClassAsync(assignment.ClassId, ct);

        var submission = await submissions.GetByAssignmentAndStudentAsync(assignmentId, studentId, ct)
                          ?? throw new NotFoundException("AssignmentSubmission", studentId);

        submission.Score = request.Score;
        submission.Feedback = request.Feedback;

        submissions.Update(submission);
        await submissions.SaveChangesAsync(ct);

        return await submissions.GetForStudentAsync(assignmentId, studentId, ct)
               ?? throw new NotFoundException("AssignmentSubmission", studentId);
    }
}