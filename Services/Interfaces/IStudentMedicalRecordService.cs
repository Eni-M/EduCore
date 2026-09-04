using EduCore.Dtos.Medical;

namespace EduCore.Services.Interfaces;

public interface IStudentMedicalRecordService
{
    // Null when the student has no record yet -- the caller is allowed to look, there is simply
    // nothing there.
    Task<StudentMedicalRecordDto?> GetByStudentAsync(Guid studentId, CancellationToken ct);

    Task<StudentMedicalRecordDto> UpsertAsync(Guid studentId, StudentMedicalRecordUpsertDto dto, CancellationToken ct);

    // A guardian updating their own child's record. Verifies the guardian link and notifies the
    // school's directorate to review the change.
    Task<StudentMedicalRecordDto> UpsertByGuardianAsync(
        Guid studentId, StudentMedicalRecordUpsertDto dto, CancellationToken ct);
}
