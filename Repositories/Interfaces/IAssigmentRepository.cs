using EduCore.Dtos.Assignment;
using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface IAssignmentRepository : IRepository<Assignment>
{
    Task<IEnumerable<AssignmentListDto>> GetByClassAsync(
        Guid classId,
        CancellationToken ct);

    Task<IEnumerable<AssignmentListDto>> GetByTeacherAsync(
        Guid teacherId,
        CancellationToken ct);

    Task<AssignmentDetailsDto?> GetDetailsAsync(
        Guid assignmentId,
        CancellationToken ct);

    Task<IEnumerable<AssignmentCalendarDto>> GetForCalendarAsync(
       Guid[] classIds,
       DateOnly? from,
       DateOnly? to,
       CancellationToken ct);
}