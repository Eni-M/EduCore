using EduCore.Dtos.SchoolingHistory;

namespace EduCore.Services.Interfaces;

public interface ISchoolingHistoryService
{
    Task<IEnumerable<SchoolingHistoryListDto>> GetByStudentAsync(Guid studentId, CancellationToken ct);
    Task<SchoolingHistoryListDto> CreateAsync(Guid studentId, SchoolingHistoryCreateDto dto, CancellationToken ct);
    Task<SchoolingHistoryListDto> UpdateAsync(Guid studentId, Guid historyId, SchoolingHistoryEditDto dto, CancellationToken ct);
    Task DeleteAsync(Guid studentId, Guid historyId, CancellationToken ct);
}
