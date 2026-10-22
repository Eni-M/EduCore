using AutoMapper;
using EduCore.Dtos.AcademicYear;
using EduCore.Dtos.GradingPeriod;
using EduCore.Dtos.Pagination;
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
    public async Task<IEnumerable<AcademicYearListDto>> GetAllAsync(Guid? schoolId, CancellationToken ct)
    {
        // Neither the ministry nor a regional admin carries a school_id claim, and the school
        // scope filter is "SchoolIds contains this row's school" -- so the ordinary listing
        // returns nothing at all for them, and the schoolId they asked to filter by was being
        // dropped on the floor. They read across schools instead.
        if (currentUser.IsStateAdmin || currentUser.IsRegionalAdmin)
            return await academicYears.GetAllAcrossSchoolsAsync(
                schoolId,
                currentUser.IsRegionalAdmin ? currentUser.RequireCityId() : null,
                ct);

        return mapper.Map<IEnumerable<AcademicYearListDto>>(await academicYears.GetAllOrderedAsync(ct));
    }

    public async Task<PagedResultDto<AcademicYearListDto>> GetPagedAsync(
        Guid? schoolId, PaginationQueryDto pagination, string? search, CancellationToken ct)
    {
        var all = await GetAllAsync(schoolId, ct);
        return all.Search(search, y => new[] { y.Name, y.SchoolName })
            .ToPagedResult(pagination);
    }

    public async Task<AcademicYearListDto> GetByIdAsync(Guid academicYearId, CancellationToken ct) =>
        mapper.Map<AcademicYearListDto>(await GetOwnedYearAsync(academicYearId, ct));

    public async Task<AcademicYearListDto> CreateAsync(AcademicYearCreateDto dto, CancellationToken ct)
    {
        if (await academicYears.NameExistsAsync(dto.Name, null, ct))
            throw new ConflictException($"An academic year named {dto.Name} already exists in this school.");

        if (await academicYears.OverlapsAsync(dto.StartDate, dto.EndDate, null, ct))
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

    public async Task<AcademicYearListDto> UpdateAsync(
        Guid academicYearId, AcademicYearCreateDto dto, CancellationToken ct)
    {
        var academicYear = await GetOwnedYearAsync(academicYearId, ct);

        if (await academicYears.NameExistsAsync(dto.Name, academicYearId, ct))
            throw new ConflictException($"An academic year named {dto.Name} already exists in this school.");

        if (await academicYears.OverlapsAsync(dto.StartDate, dto.EndDate, academicYearId, ct))
            throw new ConflictException("This academic year overlaps an existing one.");

        // Terms are dated inside their year. Shrinking the year out from under one would leave
        // marks sitting in a term that no longer belongs to any year, so it is refused here
        // rather than silently corrected -- move the terms first.
        var periods = (await gradingPeriods.GetByAcademicYearAsync(academicYearId, ct)).ToList();

        var stranded = periods
            .Where(p => p.StartDate < dto.StartDate || p.EndDate > dto.EndDate)
            .Select(p => p.Name)
            .ToList();

        if (stranded.Count > 0)
            throw new BusinessException(
                $"These terms would fall outside the year: {string.Join(", ", stranded)}. " +
                "Move them first, then change the year.");

        academicYear.Name = dto.Name;
        academicYear.StartDate = dto.StartDate;
        academicYear.EndDate = dto.EndDate;

        academicYears.Update(academicYear);
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

        return ToDto(gradingPeriod, academicYear);
    }

    // Closing a term freezes its marks for teachers; the office may still correct one, with a
    // reason that MarkService keeps. Reopening is allowed -- mistakes happen -- and is itself
    // audited, since the period's ClosedAt change is a value-audited write.
    public async Task<GradingPeriodListDto> SetGradingPeriodClosedAsync(
        Guid gradingPeriodId, bool closed, CancellationToken ct)
    {
        var gradingPeriod = await gradingPeriods.GetByIdAsync(gradingPeriodId, ct)
                            ?? throw new NotFoundException("Grading period", gradingPeriodId);

        var academicYear = await GetOwnedYearAsync(gradingPeriod.AcademicYearId, ct);

        if (gradingPeriod.IsClosed != closed)
        {
            gradingPeriod.ClosedAt = closed ? DateTime.UtcNow : null;
            gradingPeriod.ClosedByUserId = closed ? currentUser.UserId : null;
            gradingPeriods.Update(gradingPeriod);
            await gradingPeriods.SaveChangesAsync(ct);
        }

        return ToDto(gradingPeriod, academicYear);
    }

    private static GradingPeriodListDto ToDto(GradingPeriod gradingPeriod, AcademicYear academicYear) =>
        new(
            gradingPeriod.Id,
            academicYear.Id,
            academicYear.Name,
            gradingPeriod.Name,
            gradingPeriod.StartDate,
            gradingPeriod.EndDate,
            gradingPeriod.ClosedAt);

    private async Task<AcademicYear> GetOwnedYearAsync(Guid academicYearId, CancellationToken ct) =>
        await academicYears.GetByIdAsync(academicYearId, ct)
        ?? throw new NotFoundException("Academic year", academicYearId);
}
