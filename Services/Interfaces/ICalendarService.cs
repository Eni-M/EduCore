using EduCore.Dtos.Calendar;

namespace EduCore.Services.Interfaces;

public interface ICalendarService
{
    // The events that matter to the caller, over an optional date window.
    Task<IEnumerable<CalendarEventListDto>> GetMineAsync(DateOnly? from, DateOnly? to, CancellationToken ct);

    Task<CalendarEventListDto> CreateAsync(CalendarEventCreateDto dto, CancellationToken ct);
    Task<CalendarEventListDto> UpdateAsync(Guid eventId, CalendarEventEditDto dto, CancellationToken ct);
    Task DeleteAsync(Guid eventId, CancellationToken ct);
}
