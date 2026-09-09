using EduCore.Dtos.Assignment;
using EduCore.Exceptions;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Services.Assignments;

public class AssignmentSubmissionService(
    IAssignmentSubmissionRepository submissions,
    IAssignmentRepository assignments,
    ICurrentUser currentUser) : IAssignmentSubmissionService
{
    public async Task<IEnumerable<AssignmentSubmissionDto>> GetForAssignmentAsync(
        Guid assignmentId,
        CancellationToken ct)
    {
        var assignment = await assignments.GetByIdAsync(assignmentId, ct)
                          ?? throw new NotFoundException("Assignment", assignmentId);

        if (assignment.TeacherId != currentUser.RequireTeacherId())
            throw new ForbiddenException("You are not the teacher for this assignment.");

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

        if (assignment.TeacherId != currentUser.RequireTeacherId())
            throw new ForbiddenException("You are not the teacher for this assignment.");

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