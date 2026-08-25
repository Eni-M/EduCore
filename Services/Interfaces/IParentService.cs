using EduCore.Dtos.Parent;

namespace EduCore.Services.Interfaces;

public interface IParentService
{
    Task<IEnumerable<ParentListDto>> GetByStudentAsync(Guid studentId, CancellationToken ct);
    Task<ParentListDto> CreateAsync(ParentCreateDto dto, CancellationToken ct);
    Task<ParentListDto> UpdateAsync(Guid parentId, ParentEditDto dto, CancellationToken ct);
    Task DeleteAsync(Guid parentId, CancellationToken ct);
}
