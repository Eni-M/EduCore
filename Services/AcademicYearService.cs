using AutoMapper;
using EduCore.Dtos.AcademicYear;
using EduCore.Dtos.GradingPeriod;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Helpers;
using EduCore.Services.Interfaces;

namespace EduCore.Services;

public class AcademicYearService(
    IAcademicYearRepository academicYears,
    IGradingPeriodRepository gradingPeriods,
    IAcademicCalendar calendar,
    ICurrentUser currentUser,
    IMapper mapper) : IAcademicYearService
{
    public async Task<IEnumerable<AcademicYearListDto>> GetAllAsync(Guid? schoolId, CancellationToken ct) =>
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

        var (firstStart, firstEnd) = calendar.FirstPeriod(dto.StartDate, dto.EndDate);
        var (secondStart, secondEnd) = calendar.SecondPeriod(dto.StartDate, dto.EndDate);

        await gradingPeriods.AddAsync(new GradingPeriod
        {
            AcademicYearId = academicYear.Id,
            SchoolId = academicYear.SchoolId,
            Name = "First term",
            StartDate = firstStart,
            EndDate = firstEnd
        });

        await gradingPeriods.AddAsync(new GradingPeriod
        {
            AcademicYearId = academicYear.Id,
            SchoolId = academicYear.SchoolId,
            Name = "Second term",
            StartDate = secondStart,
            EndDate = secondEnd
        });

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

    public async Task<GradingPeriodListDto> EditGradingPeriodAsync(
        Guid gradingPeriodId,
        GradingPeriodEditDto dto,
        CancellationToken ct)
    {
        var gradingPeriod = await gradingPeriods.GetByIdAsync(gradingPeriodId, ct)
                            ?? throw new NotFoundException("Grading period", gradingPeriodId);

        var academicYear = await GetOwnedYearAsync(gradingPeriod.AcademicYearId, ct);

        if (dto.StartDate < academicYear.StartDate || dto.EndDate > academicYear.EndDate)
            throw new BusinessException(
                $"A grading period must fall inside its academic year ({academicYear.StartDate} - {academicYear.EndDate}).");

        var otherPeriod = (await gradingPeriods.GetByAcademicYearAsync(academicYear.Id, ct))
            .FirstOrDefault(p => p.Id != gradingPeriodId);

        if (otherPeriod is not null && dto.StartDate <= otherPeriod.EndDate && otherPeriod.StartDate <= dto.EndDate)
            throw new ConflictException("This would overlap the academic year's other grading period.");

        gradingPeriod.StartDate = dto.StartDate;
        gradingPeriod.EndDate = dto.EndDate;

        gradingPeriods.Update(gradingPeriod);
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
