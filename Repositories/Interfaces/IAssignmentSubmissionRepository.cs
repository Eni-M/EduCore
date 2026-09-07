using EduCore.Dtos.Assignment;
using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface IAssignmentSubmissionRepository : IRepository<AssignmentSubmission>
{
    Task<AssignmentSubmission?> GetByAssignmentAndStudentAsync(
        Guid assignmentId,
        Guid studentId,
        CancellationToken ct);

    Task<AssignmentSubmissionDto?> GetForStudentAsync(
        Guid assignmentId,
        Guid studentId,
        CancellationToken ct);

    Task<IEnumerable<AssignmentSubmissionDto>> GetForAssignmentAsync(
        Guid assignmentId,
        CancellationToken ct);
}