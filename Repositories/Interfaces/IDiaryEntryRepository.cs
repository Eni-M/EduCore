using EduCore.Dtos.DiaryEntry;
using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface IDiaryEntryRepository : IRepository<DiaryEntry>
{
    Task<IEnumerable<DiaryEntryListDto>> GetByClassAsync(
        Guid classId, DateOnly? from, DateOnly? to, CancellationToken ct);

    Task<IEnumerable<DiaryEntryListDto>> GetByTeacherAsync(
        Guid teacherId, DateOnly? from, DateOnly? to, CancellationToken ct);

    Task<DiaryEntryDetailDto?> GetDetailAsync(Guid diaryEntryId, CancellationToken ct);

    Task<bool> SlotTakenAsync(Guid classId, DateOnly date, int period, CancellationToken ct);
}
