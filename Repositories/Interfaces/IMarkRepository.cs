using EduCore.Dtos.Mark;
using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface IMarkRepository : IRepository<Mark>
{
    Task<IEnumerable<MarkListDto>> GetByStudentAsync(
        Guid studentId, Guid? subjectId, Guid? gradingPeriodId, CancellationToken ct);

    Task<IEnumerable<MarkListDto>> GetByClassAsync(
        Guid classId, Guid? subjectId, Guid? gradingPeriodId, CancellationToken ct);

    Task<MarkListDto?> GetDtoAsync(Guid markId, CancellationToken ct);
}
