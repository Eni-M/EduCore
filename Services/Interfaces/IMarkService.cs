using EduCore.Dtos.Mark;

namespace EduCore.Services.Interfaces;

public interface IMarkService
{
    Task<IEnumerable<MarkListDto>> GetByStudentAsync(
        Guid studentId, Guid? subjectId, Guid? gradingPeriodId, CancellationToken ct);

    Task<IEnumerable<MarkListDto>> GetByClassAsync(
        Guid classId, Guid? subjectId, Guid? gradingPeriodId, CancellationToken ct);

    Task<MarkListDto> UpsertAsync(MarkUpsertDto dto, CancellationToken ct);
}
