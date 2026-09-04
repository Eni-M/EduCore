using AutoMapper;
using EduCore.Dtos.Pagination;
using EduCore.Dtos.StateAdmin;
using EduCore.Enums;
using EduCore.Exceptions;
using EduCore.Helpers;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Services.Interfaces;

namespace EduCore.Services;

public class StateAdminService(
    IStateAdminRepository stateAdmins,
    ILocationRepository locations,
    IUserRepository userRepository,
    IUserService users,
    IMapper mapper) : IStateAdminService
{
    public async Task<IEnumerable<StateAdminListDto>> GetAllAsync(CancellationToken ct) =>
        mapper.Map<IEnumerable<StateAdminListDto>>(await stateAdmins.GetAllWithUserAsync(ct));

    public async Task<PagedResultDto<StateAdminListDto>> GetPagedAsync(
        PaginationQueryDto pagination, string? search, CancellationToken ct)
    {
        var all = await GetAllAsync(ct);
        return all.Search(search, a => new[] { a.FirstName, a.LastName, a.Email, a.StateName })
            .ToPagedResult(pagination);
    }

    public async Task<StateAdminListDto> GetByIdAsync(Guid stateAdminId, CancellationToken ct)
    {
        var stateAdmin = await stateAdmins.GetWithUserAsync(stateAdminId, ct)
                         ?? throw new NotFoundException("State administrator", stateAdminId);

        return mapper.Map<StateAdminListDto>(stateAdmin);
    }

    public async Task<StateAdminListDto> CreateAsync(StateAdminCreateDto dto, CancellationToken ct)
    {
        _ = await locations.GetStateAsync(dto.StateId, ct)
            ?? throw new NotFoundException("State", dto.StateId);

        var user = await users.BuildLoginAsync(
            dto.FirstName, dto.LastName, dto.Email, dto.Password, UserRole.StateAdmin, ct);

        var stateAdmin = new StateAdmin { User = user, StateId = dto.StateId };

        await stateAdmins.AddAsync(stateAdmin);
        await stateAdmins.SaveChangesAsync(ct);

        return await GetByIdAsync(stateAdmin.Id, ct);
    }

    public async Task<StateAdminListDto> UpdateAsync(Guid stateAdminId, StateAdminEditDto dto, CancellationToken ct)
    {
        var stateAdmin = await stateAdmins.GetByIdAsync(stateAdminId, ct)
                         ?? throw new NotFoundException("State administrator", stateAdminId);

        _ = await locations.GetStateAsync(dto.StateId, ct)
            ?? throw new NotFoundException("State", dto.StateId);

        var user = await userRepository.GetByIdAsync(stateAdmin.UserId, ct)
                   ?? throw new NotFoundException("User", stateAdmin.UserId);

        await users.ApplyLoginEditAsync(user, dto.FirstName, dto.LastName, dto.Email, ct);

        stateAdmin.StateId = dto.StateId;
        stateAdmins.Update(stateAdmin);
        await stateAdmins.SaveChangesAsync(ct);

        return await GetByIdAsync(stateAdminId, ct);
    }
}
