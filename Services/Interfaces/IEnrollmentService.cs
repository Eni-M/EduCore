using EduCore.Dtos.Enrollment;

namespace EduCore.Services.Interfaces;

public interface IEnrollmentService
{
    Task<IEnumerable<EnrollmentListDto>> GetByClassAsync(Guid classId, bool includeLeft, CancellationToken ct);
    Task<IEnumerable<EnrollmentListDto>> GetByStudentAsync(Guid studentId, CancellationToken ct);

    Task<EnrollmentListDto> EnrollAsync(EnrollmentCreateDto dto, CancellationToken ct);
    Task WithdrawAsync(Guid enrollmentId, EnrollmentWithdrawDto dto, CancellationToken ct);
}
