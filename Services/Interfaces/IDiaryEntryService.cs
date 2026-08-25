using EduCore.Dtos.DiaryEntry;

namespace EduCore.Services.Interfaces;

public interface IDiaryEntryService
{
    Task<IEnumerable<DiaryEntryListDto>> GetByClassAsync(
        Guid classId, DateOnly? from, DateOnly? to, CancellationToken ct);

    Task<IEnumerable<DiaryEntryListDto>> GetMineAsync(DateOnly? from, DateOnly? to, CancellationToken ct);

    Task<DiaryEntryDetailDto> GetDetailAsync(Guid diaryEntryId, CancellationToken ct);

    Task<DiaryEntryDetailDto> CreateAsync(DiaryEntryCreateDto dto, CancellationToken ct);

    Task<DiaryEntryDetailDto> UpdateAsync(Guid diaryEntryId, DiaryEntryEditDto dto, CancellationToken ct);
}
