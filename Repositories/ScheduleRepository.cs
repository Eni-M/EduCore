using EduCore.Data;
using EduCore.Dtos.Schedule;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class ScheduleRepository(AppDbContext context) : IScheduleRepository
{
    // Shifts -----------------------------------------------------------------

    public async Task<IReadOnlyList<SchoolShift>> GetShiftsAsync(CancellationToken ct) =>
        await context.SchoolShifts
            .AsNoTracking()
            .Include(s => s.Periods)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

    public async Task<SchoolShift?> GetShiftAsync(Guid shiftId, CancellationToken ct) =>
        await context.SchoolShifts
            .AsNoTracking()
            .Include(s => s.Periods)
            .FirstOrDefaultAsync(s => s.Id == shiftId, ct);

    public async Task<SchoolShift?> GetShiftTrackedAsync(Guid shiftId, CancellationToken ct) =>
        await context.SchoolShifts
            .Include(s => s.Periods)
            .FirstOrDefaultAsync(s => s.Id == shiftId, ct);

    public async Task<bool> ShiftNameExistsAsync(string name, Guid? excludeId, CancellationToken ct) =>
        await context.SchoolShifts
            .AnyAsync(s => s.Name == name && (excludeId == null || s.Id != excludeId), ct);

    public async Task AddShiftAsync(SchoolShift shift) => await context.SchoolShifts.AddAsync(shift);
    public void UpdateShift(SchoolShift shift) => context.SchoolShifts.Update(shift);
    public void RemoveShift(SchoolShift shift) => context.SchoolShifts.Remove(shift);

    // Periods ----------------------------------------------------------------

    public async Task<ShiftPeriod?> GetPeriodAsync(Guid periodId, CancellationToken ct) =>
        await context.ShiftPeriods.AsNoTracking().FirstOrDefaultAsync(p => p.Id == periodId, ct);

    public async Task<ShiftPeriod?> GetPeriodTrackedAsync(Guid periodId, CancellationToken ct) =>
        await context.ShiftPeriods.FirstOrDefaultAsync(p => p.Id == periodId, ct);

    public async Task<bool> PeriodNumberExistsAsync(
        Guid shiftId, int periodNumber, Guid? excludeId, CancellationToken ct) =>
        await context.ShiftPeriods.AnyAsync(
            p => p.SchoolShiftId == shiftId && p.PeriodNumber == periodNumber
                 && (excludeId == null || p.Id != excludeId), ct);

    public async Task AddPeriodAsync(ShiftPeriod period) => await context.ShiftPeriods.AddAsync(period);
    public void UpdatePeriod(ShiftPeriod period) => context.ShiftPeriods.Update(period);
    public void RemovePeriod(ShiftPeriod period) => context.ShiftPeriods.Remove(period);

    // Entries ----------------------------------------------------------------

    private IQueryable<ScheduleEntryDto> Project(IQueryable<ScheduleEntry> entries) =>
        from e in entries
        join c in context.Classes on e.ClassId equals c.Id
        join s in context.Subjects on e.SubjectId equals s.Id
        join t in context.Teachers on e.TeacherId equals t.Id
        join u in context.Users on t.UserId equals u.Id
        join p in context.ShiftPeriods on e.ShiftPeriodId equals p.Id
        select new ScheduleEntryDto(
            e.Id,
            e.ClassId, c.Name,
            e.SubjectId, s.Name,
            e.TeacherId, u.FirstName + " " + u.LastName,
            e.ShiftPeriodId, p.PeriodNumber, p.StartTime, p.EndTime,
            e.DayOfWeek);

    public async Task<ScheduleEntryDto?> GetEntryDtoAsync(Guid entryId, CancellationToken ct) =>
        await Project(context.ScheduleEntries.AsNoTracking().Where(e => e.Id == entryId))
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<ScheduleEntryDto>> GetEntriesByClassAsync(Guid classId, CancellationToken ct) =>
        await Project(context.ScheduleEntries.AsNoTracking().Where(e => e.ClassId == classId))
            .OrderBy(e => e.DayOfWeek).ThenBy(e => e.PeriodNumber)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ScheduleEntryDto>> GetEntriesByTeacherAsync(Guid teacherId, CancellationToken ct) =>
        await Project(context.ScheduleEntries.AsNoTracking().Where(e => e.TeacherId == teacherId))
            .OrderBy(e => e.DayOfWeek).ThenBy(e => e.PeriodNumber)
            .ToListAsync(ct);

    public async Task<ScheduleEntry?> GetEntryTrackedAsync(Guid entryId, CancellationToken ct) =>
        await context.ScheduleEntries.FirstOrDefaultAsync(e => e.Id == entryId, ct);

    public async Task<bool> ClassSlotTakenAsync(
        Guid academicYearId, Guid classId, DayOfWeek day, Guid periodId, CancellationToken ct) =>
        await context.ScheduleEntries.AnyAsync(
            e => e.AcademicYearId == academicYearId && e.ClassId == classId
                 && e.DayOfWeek == day && e.ShiftPeriodId == periodId, ct);

    public async Task<bool> TeacherSlotTakenAsync(
        Guid academicYearId, Guid teacherId, DayOfWeek day, Guid periodId, CancellationToken ct) =>
        await context.ScheduleEntries.AnyAsync(
            e => e.AcademicYearId == academicYearId && e.TeacherId == teacherId
                 && e.DayOfWeek == day && e.ShiftPeriodId == periodId, ct);

    public async Task AddEntryAsync(ScheduleEntry entry) => await context.ScheduleEntries.AddAsync(entry);
    public void RemoveEntry(ScheduleEntry entry) => context.ScheduleEntries.Remove(entry);

    public async Task<bool> SaveChangesAsync(CancellationToken ct) => await context.SaveChangesAsync(ct) > 0;
}
