using EduCore.Dtos.Schedule;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Services;

public class ScheduleService(
    IScheduleRepository schedule,
    IClassRepository classes,
    IClassSubjectTeacherRepository assignments,
    IAccessGuard access,
    ICurrentUser currentUser) : IScheduleService
{
    // Shifts -----------------------------------------------------------------

    public async Task<IEnumerable<SchoolShiftDto>> GetShiftsAsync(CancellationToken ct) =>
        (await schedule.GetShiftsAsync(ct)).Select(ToShiftDto);

    public async Task<SchoolShiftDto> CreateShiftAsync(SchoolShiftCreateDto dto, CancellationToken ct)
    {
        var schoolId = currentUser.RequireSchoolId();
        var name = dto.Name.Trim();

        if (await schedule.ShiftNameExistsAsync(name, null, ct))
            throw new ConflictException("A shift with this name already exists.");

        var shift = new SchoolShift { SchoolId = schoolId, Name = name, IsActive = true };
        await schedule.AddShiftAsync(shift);
        await schedule.SaveChangesAsync(ct);

        return ToShiftDto(shift);
    }

    public async Task<SchoolShiftDto> UpdateShiftAsync(Guid shiftId, SchoolShiftEditDto dto, CancellationToken ct)
    {
        var shift = await schedule.GetShiftTrackedAsync(shiftId, ct)
                    ?? throw new NotFoundException("Shift", shiftId);

        var name = dto.Name.Trim();
        if (await schedule.ShiftNameExistsAsync(name, shiftId, ct))
            throw new ConflictException("A shift with this name already exists.");

        shift.Name = name;
        shift.IsActive = dto.IsActive;
        schedule.UpdateShift(shift);
        await schedule.SaveChangesAsync(ct);

        return ToShiftDto(shift);
    }

    public async Task DeleteShiftAsync(Guid shiftId, CancellationToken ct)
    {
        var shift = await schedule.GetShiftTrackedAsync(shiftId, ct)
                    ?? throw new NotFoundException("Shift", shiftId);

        schedule.RemoveShift(shift);
        await SaveOrConflict(ct, "This shift's periods are still used by a timetable. Remove those lessons first.");
    }

    // Periods ----------------------------------------------------------------

    public async Task<ShiftPeriodDto> AddPeriodAsync(Guid shiftId, ShiftPeriodWriteDto dto, CancellationToken ct)
    {
        var schoolId = currentUser.RequireSchoolId();

        var shift = await schedule.GetShiftAsync(shiftId, ct)
                    ?? throw new NotFoundException("Shift", shiftId);

        await ValidatePeriodAsync(shiftId, dto, null, shift.Periods, ct);

        var period = new ShiftPeriod
        {
            SchoolId = schoolId,
            SchoolShiftId = shiftId,
            PeriodNumber = dto.PeriodNumber,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime
        };
        await schedule.AddPeriodAsync(period);
        await schedule.SaveChangesAsync(ct);

        return ToPeriodDto(period);
    }

    public async Task<ShiftPeriodDto> UpdatePeriodAsync(Guid periodId, ShiftPeriodWriteDto dto, CancellationToken ct)
    {
        var period = await schedule.GetPeriodTrackedAsync(periodId, ct)
                     ?? throw new NotFoundException("Period", periodId);

        var siblings = (await schedule.GetShiftAsync(period.SchoolShiftId, ct))?.Periods
                       ?? new List<ShiftPeriod>();

        await ValidatePeriodAsync(period.SchoolShiftId, dto, periodId, siblings, ct);

        period.PeriodNumber = dto.PeriodNumber;
        period.StartTime = dto.StartTime;
        period.EndTime = dto.EndTime;
        schedule.UpdatePeriod(period);
        await schedule.SaveChangesAsync(ct);

        return ToPeriodDto(period);
    }

    public async Task DeletePeriodAsync(Guid periodId, CancellationToken ct)
    {
        var period = await schedule.GetPeriodTrackedAsync(periodId, ct)
                     ?? throw new NotFoundException("Period", periodId);

        schedule.RemovePeriod(period);
        await SaveOrConflict(ct, "This period is still used by a timetable. Remove those lessons first.");
    }

    // Entries ----------------------------------------------------------------

    public async Task<IEnumerable<ScheduleEntryDto>> GetClassTimetableAsync(Guid classId, CancellationToken ct)
    {
        await access.EnsureCanViewClassAsync(classId, ct);
        return await schedule.GetEntriesByClassAsync(classId, ct);
    }

    public async Task<IEnumerable<ScheduleEntryDto>> GetTeacherTimetableAsync(Guid teacherId, CancellationToken ct) =>
        await schedule.GetEntriesByTeacherAsync(teacherId, ct);

    public async Task<ScheduleEntryDto> CreateEntryAsync(ScheduleEntryCreateDto dto, CancellationToken ct)
    {
        var schoolId = currentUser.RequireSchoolId();

        var cls = await classes.GetByIdAsync(dto.ClassId, ct)
                  ?? throw new NotFoundException("Class", dto.ClassId);

        var period = await schedule.GetPeriodAsync(dto.ShiftPeriodId, ct)
                     ?? throw new NotFoundException("Period", dto.ShiftPeriodId);

        // The active assignment is the single source of truth for who teaches this subject here.
        var assignment = await assignments.GetActiveAsync(dto.ClassId, dto.SubjectId, ct)
            ?? throw new BusinessException(
                "No teacher is assigned to teach this subject for this class. Assign one first.");

        if (await schedule.ClassSlotTakenAsync(cls.AcademicYearId, dto.ClassId, dto.DayOfWeek, dto.ShiftPeriodId, ct))
            throw new ConflictException("This class already has a lesson in that slot.");

        if (await schedule.TeacherSlotTakenAsync(
                cls.AcademicYearId, assignment.TeacherId, dto.DayOfWeek, dto.ShiftPeriodId, ct))
            throw new ConflictException("That teacher already teaches another class in that slot.");

        var entry = new ScheduleEntry
        {
            SchoolId = schoolId,
            AcademicYearId = cls.AcademicYearId,
            ClassId = dto.ClassId,
            SubjectId = dto.SubjectId,
            TeacherId = assignment.TeacherId,
            ShiftPeriodId = dto.ShiftPeriodId,
            DayOfWeek = dto.DayOfWeek
        };
        await schedule.AddEntryAsync(entry);
        await SaveOrConflict(ct, "That slot is already taken.");

        return await schedule.GetEntryDtoAsync(entry.Id, ct)
               ?? throw new NotFoundException("ScheduleEntry", entry.Id);
    }

    public async Task DeleteEntryAsync(Guid entryId, CancellationToken ct)
    {
        var entry = await schedule.GetEntryTrackedAsync(entryId, ct)
                    ?? throw new NotFoundException("ScheduleEntry", entryId);

        schedule.RemoveEntry(entry);
        await schedule.SaveChangesAsync(ct);
    }

    // Helpers ----------------------------------------------------------------

    private async Task ValidatePeriodAsync(
        Guid shiftId, ShiftPeriodWriteDto dto, Guid? excludeId,
        IEnumerable<ShiftPeriod> siblings, CancellationToken ct)
    {
        if (dto.EndTime <= dto.StartTime)
            throw new BusinessException("A period's end time must be after its start time.");

        if (await schedule.PeriodNumberExistsAsync(shiftId, dto.PeriodNumber, excludeId, ct))
            throw new ConflictException($"This shift already has a period {dto.PeriodNumber}.");

        var overlaps = siblings.Any(p =>
            p.Id != excludeId && dto.StartTime < p.EndTime && p.StartTime < dto.EndTime);

        if (overlaps)
            throw new ConflictException("This period overlaps another period in the shift.");
    }

    private async Task SaveOrConflict(CancellationToken ct, string message)
    {
        try
        {
            await schedule.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            throw new ConflictException(message);
        }
    }

    private static SchoolShiftDto ToShiftDto(SchoolShift s) =>
        new(s.Id, s.Name, s.IsActive,
            s.Periods.OrderBy(p => p.PeriodNumber).Select(ToPeriodDto).ToList());

    private static ShiftPeriodDto ToPeriodDto(ShiftPeriod p) =>
        new(p.Id, p.SchoolShiftId, p.PeriodNumber, p.StartTime, p.EndTime);
}
