using EduCore.Dtos.Guardian;

namespace EduCore.Services.Interfaces;

public interface IGuardianService
{
    Task<IEnumerable<GuardianContactDto>> GetByStudentAsync(Guid studentId, CancellationToken ct);
    Task LinkAsync(Guid studentId, Guid guardianId, CancellationToken ct);
    Task UnlinkAsync(Guid studentId, Guid guardianId, CancellationToken ct);
    Task<IEnumerable<GuardianSearchResultDto>> SearchAsync(string query, CancellationToken ct);

    Task<IEnumerable<GuardianListDto>> GetAllAsync(CancellationToken ct);
    Task<GuardianListDto> GetByIdAsync(Guid guardianId, CancellationToken ct);
    Task<GuardianListDto> CreateAsync(GuardianCreateDto dto, CancellationToken ct);
    Task<GuardianListDto> UpdateAsync(Guid guardianId, GuardianEditDto dto, CancellationToken ct);
    Task<GuardianListDto> SetChildrenAsync(Guid guardianId, GuardianLinksDto dto, CancellationToken ct);

    Task<IEnumerable<GuardianChildDto>> GetMyChildrenAsync(CancellationToken ct);
}
