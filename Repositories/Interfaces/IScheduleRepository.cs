using EduCore.Dtos.Schedule;
using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface IScheduleRepository
{
    // Shifts (+ periods)
    Task<IReadOnlyList<SchoolShift>> GetShiftsAsync(CancellationToken ct);
    Task<SchoolShift?> GetShiftAsync(Guid shiftId, CancellationToken ct);
    Task<SchoolShift?> GetShiftTrackedAsync(Guid shiftId, CancellationToken ct);
    Task<bool> ShiftNameExistsAsync(string name, Guid? excludeId, CancellationToken ct);
    Task AddShiftAsync(SchoolShift shift);
    void UpdateShift(SchoolShift shift);
    void RemoveShift(SchoolShift shift);

    // Periods
    Task<ShiftPeriod?> GetPeriodAsync(Guid periodId, CancellationToken ct);
    Task<ShiftPeriod?> GetPeriodTrackedAsync(Guid periodId, CancellationToken ct);
    Task<bool> PeriodNumberExistsAsync(Guid shiftId, int periodNumber, Guid? excludeId, CancellationToken ct);
    Task AddPeriodAsync(ShiftPeriod period);
    void UpdatePeriod(ShiftPeriod period);
    void RemovePeriod(ShiftPeriod period);

    // Entries
    Task<ScheduleEntryDto?> GetEntryDtoAsync(Guid entryId, CancellationToken ct);
    Task<IReadOnlyList<ScheduleEntryDto>> GetEntriesByClassAsync(Guid classId, CancellationToken ct);
    Task<IReadOnlyList<ScheduleEntryDto>> GetEntriesByTeacherAsync(Guid teacherId, CancellationToken ct);
    Task<ScheduleEntry?> GetEntryTrackedAsync(Guid entryId, CancellationToken ct);
    Task<bool> ClassSlotTakenAsync(Guid academicYearId, Guid classId, DayOfWeek day, Guid periodId, CancellationToken ct);
    Task<bool> TeacherSlotTakenAsync(Guid academicYearId, Guid teacherId, DayOfWeek day, Guid periodId, CancellationToken ct);
    Task AddEntryAsync(ScheduleEntry entry);
    void RemoveEntry(ScheduleEntry entry);

    Task<bool> SaveChangesAsync(CancellationToken ct);
}
