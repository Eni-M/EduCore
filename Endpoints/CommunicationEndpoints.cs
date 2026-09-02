using EduCore.Dtos.Calendar;
using EduCore.Dtos.Notice;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Endpoints;

// The school's outward communication: the notice/news board and the shared calendar. Everyone in
// the school reads both; the Directorate owns the board, and the Directorate and teachers put
// things on the calendar.
public static class CommunicationEndpoints
{
    public static void MapNoticeEndpoints(this IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/notices").WithTags("Notices");

        group.MapGet("/", async (INoticeService service, CancellationToken ct) =>
                Results.Ok(await service.GetForSchoolAsync(ct)))
            .RequireAuthorization(Policies.StudentReadable);

        group.MapPost("/", async (NoticeCreateDto dto, INoticeService service, CancellationToken ct) =>
            {
                var notice = await service.CreateAsync(dto, ct);
                return Results.Created($"/api/notices/{notice.Id}", notice);
            })
            .RequireAuthorization(Policies.Directorate);

        group.MapPut("/{noticeId:guid}",
                async (Guid noticeId, NoticeEditDto dto, INoticeService service, CancellationToken ct) =>
                    Results.Ok(await service.UpdateAsync(noticeId, dto, ct)))
            .RequireAuthorization(Policies.Directorate);

        group.MapDelete("/{noticeId:guid}", async (Guid noticeId, INoticeService service, CancellationToken ct) =>
            {
                await service.DeleteAsync(noticeId, ct);
                return Results.NoContent();
            })
            .RequireAuthorization(Policies.Directorate);
    }

    public static void MapCalendarEndpoints(this IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/calendar").WithTags("Calendar");

        // The personalized feed -- what turns up depends on who is asking.
        group.MapGet("/mine", async (DateOnly? from, DateOnly? to, ICalendarService service, CancellationToken ct) =>
                Results.Ok(await service.GetMineAsync(from, to, ct)))
            .RequireAuthorization(Policies.StudentReadable);

        group.MapPost("/", async (CalendarEventCreateDto dto, ICalendarService service, CancellationToken ct) =>
            {
                var ev = await service.CreateAsync(dto, ct);
                return Results.Created($"/api/calendar/{ev.Id}", ev);
            })
            .RequireAuthorization(Policies.DirectorateOrTeacher);

        group.MapPut("/{eventId:guid}",
                async (Guid eventId, CalendarEventEditDto dto, ICalendarService service, CancellationToken ct) =>
                    Results.Ok(await service.UpdateAsync(eventId, dto, ct)))
            .RequireAuthorization(Policies.DirectorateOrTeacher);

        group.MapDelete("/{eventId:guid}", async (Guid eventId, ICalendarService service, CancellationToken ct) =>
            {
                await service.DeleteAsync(eventId, ct);
                return Results.NoContent();
            })
            .RequireAuthorization(Policies.DirectorateOrTeacher);
    }
}
