using EduCore.Dtos.Calendar;
using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface ICalendarEventRepository : IRepository<CalendarEvent>
{
    // The personalized feed. seeAll short-circuits the audience filter (the Directorate sees the
    // whole school's calendar); otherwise an event shows only if it is school-wide, targets one of
    // the viewer's classes or students, or was created by them.
    Task<IEnumerable<CalendarEventListDto>> GetForViewerAsync(
        bool seeAll,
        IReadOnlyCollection<Guid> classIds,
        IReadOnlyCollection<Guid> studentIds,
        Guid creatorUserId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken ct);
}
