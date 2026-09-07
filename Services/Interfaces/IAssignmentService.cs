using EduCore.Dtos.Assignment;

namespace EduCore.Services.Assignments;

public interface IAssignmentService
{
    Task<AssignmentDetailsDto> CreateAsync(
        CreateAssignmentRequest request,
        CancellationToken ct);

    Task<IEnumerable<AssignmentListDto>> GetByClassAsync(
        Guid classId,
        CancellationToken ct);

    Task<IEnumerable<AssignmentListDto>> GetByTeacherAsync(
        Guid teacherId,
        CancellationToken ct);

    Task<AssignmentDetailsDto> GetDetailsAsync(
        Guid assignmentId,
        CancellationToken ct);
}