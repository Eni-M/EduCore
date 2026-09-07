using EduCore.Dtos.Assignment;

namespace EduCore.Services.Assignments;

public interface IAssignmentSubmissionService
{
    Task<AssignmentSubmissionDto> SubmitAsync(
        Guid assignmentId,
        CreateSubmissionRequest request,
        CancellationToken ct);

    Task<IEnumerable<AssignmentSubmissionDto>> GetForAssignmentAsync(
        Guid assignmentId,
        CancellationToken ct);

    Task<AssignmentSubmissionDto> GradeAsync(
        Guid assignmentId,
        Guid studentId,
        UpdateAssignmentSubmissionDto request,
        CancellationToken ct);
}