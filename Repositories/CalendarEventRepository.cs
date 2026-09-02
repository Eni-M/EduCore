using EduCore.Data;
using EduCore.Dtos.Calendar;
using EduCore.Enums;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class CalendarEventRepository(AppDbContext context)
    : Repository<CalendarEvent>(context), ICalendarEventRepository
{
    public async Task<IEnumerable<CalendarEventListDto>> GetForViewerAsync(
        bool seeAll,
        IReadOnlyCollection<Guid> classIds,
        IReadOnlyCollection<Guid> studentIds,
        Guid creatorUserId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken ct)
    {
        var events = _context.CalendarEvents.AsNoTracking().AsQueryable();

        if (from is not null)
            events = events.Where(e => e.Date >= from);

        if (to is not null)
            events = events.Where(e => e.Date <= to);

        if (!seeAll)
            events = events.Where(e =>
                e.Audience == CalendarAudience.School
                || (e.Audience == CalendarAudience.Class && e.ClassId != null && classIds.Contains(e.ClassId.Value))
                || (e.Audience == CalendarAudience.Student && e.StudentId != null && studentIds.Contains(e.StudentId.Value))
                || e.CreatedByUserId == creatorUserId);

        return await Project(events.OrderBy(e => e.Date).ThenBy(e => e.StartTime)).ToListAsync(ct);
    }

    public async Task<CalendarEventListDto?> GetDtoAsync(Guid eventId, CancellationToken ct) =>
        await Project(_context.CalendarEvents.AsNoTracking().Where(e => e.Id == eventId)).FirstOrDefaultAsync(ct);

    // The one projection both reads share. Names are pulled with correlated subqueries so a null
    // ClassId/StudentId simply yields a null name, no outer join needed.
    private IQueryable<CalendarEventListDto> Project(IQueryable<CalendarEvent> source) =>
        from e in source
        select new CalendarEventListDto(
            e.Id,
            e.Title,
            e.Description,
            e.Type,
            e.Audience,
            e.ClassId,
            e.ClassId == null
                ? null
                : _context.Classes.Where(c => c.Id == e.ClassId).Select(c => c.Name).FirstOrDefault(),
            e.StudentId,
            e.StudentId == null
                ? null
                : (from s in _context.Students
                   where s.Id == e.StudentId
                   join u in _context.Users on s.UserId equals u.Id
                   select u.FirstName + " " + u.LastName).FirstOrDefault(),
            e.Date,
            e.StartTime,
            e.EndTime,
            e.CreatedByUserId,
            _context.Users
                .Where(u => u.Id == e.CreatedByUserId)
                .Select(u => u.FirstName + " " + u.LastName)
                .FirstOrDefault() ?? "—");
}
