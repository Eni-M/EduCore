using AutoMapper;
using EduCore.Dtos.RegionalAdmin;
using EduCore.Enums;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services.Interfaces;
using EduCore.Dtos.Pagination;
using EduCore.Helpers;

namespace EduCore.Services;

public class RegionalAdminService(
    IRegionalAdminRepository regionalAdmins,
    ILocationRepository locations,
    IUserRepository userRepository,
    IUserService users,
    ICurrentUser currentUser,
    IMapper mapper) : IRegionalAdminService
{
    public async Task<IEnumerable<RegionalAdminListDto>> GetAllAsync(CancellationToken ct) =>
        mapper.Map<IEnumerable<RegionalAdminListDto>>(
            await regionalAdmins.GetByStateAsync(currentUser.RequireStateId(), ct));

    public async Task<PagedResultDto<RegionalAdminListDto>> GetPagedAsync(
        PaginationQueryDto pagination, string? search, CancellationToken ct)
    {
        var all = await GetAllAsync(ct);
        return all.Search(search, r => new[] { r.FirstName, r.LastName, r.Email, r.CityName, r.StateName })
            .ToPagedResult(pagination);
    }

    public async Task<RegionalAdminListDto> GetByIdAsync(Guid regionalAdminId, CancellationToken ct)
    {
        var regionalAdmin = await regionalAdmins.GetWithUserAsync(regionalAdminId, ct)
                            ?? throw new NotFoundException("Regional administrator", regionalAdminId);

        EnsureInOwnState(regionalAdmin.City);

        return mapper.Map<RegionalAdminListDto>(regionalAdmin);
    }

    public async Task<RegionalAdminListDto> CreateAsync(RegionalAdminCreateDto dto, CancellationToken ct)
    {
        var city = await locations.GetCityAsync(dto.CityId, ct)
                   ?? throw new NotFoundException("City", dto.CityId);

        EnsureInOwnState(city);

        var user = await users.BuildLoginAsync(
            dto.FirstName, dto.LastName, dto.Email, dto.Password, UserRole.RegionalAdmin, ct);

        var regionalAdmin = new RegionalAdmin { User = user, CityId = dto.CityId };

        await regionalAdmins.AddAsync(regionalAdmin);
        await regionalAdmins.SaveChangesAsync(ct);

        return await GetByIdAsync(regionalAdmin.Id, ct);
    }

    public async Task<RegionalAdminListDto> UpdateAsync(
        Guid regionalAdminId, RegionalAdminEditDto dto, CancellationToken ct)
    {
        var regionalAdmin = await regionalAdmins.GetTrackedAsync(regionalAdminId, ct)
                            ?? throw new NotFoundException("Regional administrator", regionalAdminId);

        var currentCity = await locations.GetCityAsync(regionalAdmin.CityId, ct)
                          ?? throw new NotFoundException("City", regionalAdmin.CityId);
        EnsureInOwnState(currentCity);

        var targetCity = await locations.GetCityAsync(dto.CityId, ct)
                         ?? throw new NotFoundException("City", dto.CityId);
        EnsureInOwnState(targetCity);

        var user = await userRepository.GetByIdAsync(regionalAdmin.UserId, ct)
                   ?? throw new NotFoundException("User", regionalAdmin.UserId);

        await users.ApplyLoginEditAsync(user, dto.FirstName, dto.LastName, dto.Email, ct);

        regionalAdmin.CityId = dto.CityId;
        regionalAdmins.Update(regionalAdmin);
        await regionalAdmins.SaveChangesAsync(ct);

        return await GetByIdAsync(regionalAdminId, ct);
    }

    private void EnsureInOwnState(City city)
    {
        if (city.StateId != currentUser.RequireStateId())
            throw new ForbiddenException("You can only manage regional administrators for cities in your state.");
    }
}
