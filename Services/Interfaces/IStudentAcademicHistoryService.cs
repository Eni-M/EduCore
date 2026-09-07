using EduCore.Dtos.StudentHistory;

namespace EduCore.Services.Interfaces;

public interface IStudentAcademicHistoryService
{
    // The student's whole record, across every school they have attended.
    Task<StudentAcademicHistoryDto> GetAsync(Guid studentId, CancellationToken ct);

    // Recompiles the stored records from the live enrollment and mark tables. Idempotent:
    // running it twice leaves the same rows. Returns how many years were written.
    Task<int> RebuildAsync(Guid studentId, CancellationToken ct);
}
