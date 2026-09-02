namespace EduCore.Dtos.Schedule;

public record SchoolShiftDto(
    Guid Id,
    string Name,
    bool IsActive,
    IReadOnlyList<ShiftPeriodDto> Periods);
