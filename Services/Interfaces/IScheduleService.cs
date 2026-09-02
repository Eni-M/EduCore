using EduCore.Dtos.Schedule;

namespace EduCore.Services.Interfaces;

public interface IScheduleService
{
    Task<IEnumerable<SchoolShiftDto>> GetShiftsAsync(CancellationToken ct);
    Task<SchoolShiftDto> CreateShiftAsync(SchoolShiftCreateDto dto, CancellationToken ct);
    Task<SchoolShiftDto> UpdateShiftAsync(Guid shiftId, SchoolShiftEditDto dto, CancellationToken ct);
    Task DeleteShiftAsync(Guid shiftId, CancellationToken ct);

    Task<ShiftPeriodDto> AddPeriodAsync(Guid shiftId, ShiftPeriodWriteDto dto, CancellationToken ct);
    Task<ShiftPeriodDto> UpdatePeriodAsync(Guid periodId, ShiftPeriodWriteDto dto, CancellationToken ct);
    Task DeletePeriodAsync(Guid periodId, CancellationToken ct);

    Task<IEnumerable<ScheduleEntryDto>> GetClassTimetableAsync(Guid classId, CancellationToken ct);
    Task<IEnumerable<ScheduleEntryDto>> GetTeacherTimetableAsync(Guid teacherId, CancellationToken ct);
    Task<ScheduleEntryDto> CreateEntryAsync(ScheduleEntryCreateDto dto, CancellationToken ct);
    Task DeleteEntryAsync(Guid entryId, CancellationToken ct);
}
