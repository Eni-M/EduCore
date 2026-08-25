using AutoMapper;
using EduCore.Dtos.AcademicYear;
using EduCore.Dtos.GradingPeriod;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Services;

public class AcademicYearService(
    IAcademicYearRepository academicYears,
    IGradingPeriodRepository gradingPeriods,
    ICurrentUser currentUser,
    IMapper mapper) : IAcademicYearService
{
    public async Task<IEnumerable<AcademicYearListDto>> GetAllAsync(CancellationToken ct) =>
        mapper.Map<IEnumerable<AcademicYearListDto>>(await academicYears.GetAllOrderedAsync(ct));

    public async Task<AcademicYearListDto> GetByIdAsync(Guid academicYearId, CancellationToken ct) =>
        mapper.Map<AcademicYearListDto>(await GetOwnedYearAsync(academicYearId, ct));

    public async Task<AcademicYearListDto> CreateAsync(AcademicYearCreateDto dto, CancellationToken ct)
    {
        if (await academicYears.NameExistsAsync(dto.Name, ct))
            throw new ConflictException($"An academic year named {dto.Name} already exists in this school.");

        if (await academicYears.OverlapsAsync(dto.StartDate, dto.EndDate, ct))
            throw new ConflictException("This academic year overlaps an existing one.");

        var academicYear = mapper.Map<AcademicYear>(dto);
        academicYear.SchoolId = currentUser.RequireSchoolId();

        await academicYears.AddAsync(academicYear);
        await academicYears.SaveChangesAsync(ct);

        return mapper.Map<AcademicYearListDto>(academicYear);
    }

    public async Task<IEnumerable<GradingPeriodListDto>> GetGradingPeriodsAsync(
        Guid academicYearId,
        CancellationToken ct)
    {
        await GetOwnedYearAsync(academicYearId, ct);
        return await gradingPeriods.GetByAcademicYearAsync(academicYearId, ct);
    }

    public async Task<GradingPeriodListDto> CreateGradingPeriodAsync(
        GradingPeriodCreateDto dto,
        CancellationToken ct)
    {
        var academicYear = await GetOwnedYearAsync(dto.AcademicYearId, ct);

        if (dto.StartDate < academicYear.StartDate || dto.EndDate > academicYear.EndDate)
            throw new BusinessException(
                $"A grading period must fall inside its academic year ({academicYear.StartDate} - {academicYear.EndDate}).");

        if (await gradingPeriods.NameExistsInYearAsync(dto.AcademicYearId, dto.Name, ct))
            throw new ConflictException($"A grading period named {dto.Name} already exists in this academic year.");

        var gradingPeriod = mapper.Map<GradingPeriod>(dto);
        gradingPeriod.SchoolId = currentUser.RequireSchoolId();

        await gradingPeriods.AddAsync(gradingPeriod);
        await gradingPeriods.SaveChangesAsync(ct);

        return new GradingPeriodListDto(
            gradingPeriod.Id,
            academicYear.Id,
            academicYear.Name,
            gradingPeriod.Name,
            gradingPeriod.StartDate,
            gradingPeriod.EndDate);
    }

    private async Task<AcademicYear> GetOwnedYearAsync(Guid academicYearId, CancellationToken ct) =>
        await academicYears.GetByIdAsync(academicYearId, ct)
        ?? throw new NotFoundException("Academic year", academicYearId);
}
