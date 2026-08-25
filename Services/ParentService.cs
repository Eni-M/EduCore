using AutoMapper;
using EduCore.Dtos.Parent;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Services;

public class ParentService(
    IParentRepository parents,
    IStudentRepository students,
    IAccessGuard access,
    ICurrentUser currentUser,
    IMapper mapper) : IParentService
{
    public async Task<IEnumerable<ParentListDto>> GetByStudentAsync(Guid studentId, CancellationToken ct)
    {
        await access.EnsureCanViewStudentAsync(studentId, ct);
        return mapper.Map<IEnumerable<ParentListDto>>(await parents.GetByStudentAsync(studentId, ct));
    }

    public async Task<ParentListDto> CreateAsync(ParentCreateDto dto, CancellationToken ct)
    {
        _ = await students.GetByIdAsync(dto.StudentId, ct)
            ?? throw new NotFoundException("Student", dto.StudentId);

        var parent = mapper.Map<Parent>(dto);
        parent.SchoolId = currentUser.RequireSchoolId();

        await parents.AddAsync(parent);
        await parents.SaveChangesAsync(ct);

        return mapper.Map<ParentListDto>(parent);
    }

    public async Task<ParentListDto> UpdateAsync(Guid parentId, ParentEditDto dto, CancellationToken ct)
    {
        var parent = await GetOwnedAsync(parentId, ct);

        mapper.Map(dto, parent);

        parents.Update(parent);
        await parents.SaveChangesAsync(ct);

        return mapper.Map<ParentListDto>(parent);
    }

    public async Task DeleteAsync(Guid parentId, CancellationToken ct)
    {
        var parent = await GetOwnedAsync(parentId, ct);

        parents.Remove(parent);
        await parents.SaveChangesAsync(ct);
    }

    private async Task<Parent> GetOwnedAsync(Guid parentId, CancellationToken ct) =>
        await parents.GetByIdAsync(parentId, ct)
        ?? throw new NotFoundException("Parent", parentId);
}
