using EduCore.Dtos.Schedule;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Endpoints;

public static class ScheduleEndpoints
{
    public static void MapScheduleEndpoints(this IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/schedule").WithTags("Schedule");

        // Shifts + periods -------------------------------------------------
        group.MapGet("/shifts", async (IScheduleService service, CancellationToken ct) =>
                Results.Ok(await service.GetShiftsAsync(ct)))
            .RequireAuthorization(Policies.AnySchoolUser);

        group.MapPost("/shifts", async (SchoolShiftCreateDto dto, IScheduleService service, CancellationToken ct) =>
            {
                var shift = await service.CreateShiftAsync(dto, ct);
                return Results.Created($"/api/schedule/shifts/{shift.Id}", shift);
            })
            .RequireAuthorization(Policies.Directorate);

        group.MapPut("/shifts/{shiftId:guid}",
                async (Guid shiftId, SchoolShiftEditDto dto, IScheduleService service, CancellationToken ct) =>
                    Results.Ok(await service.UpdateShiftAsync(shiftId, dto, ct)))
            .RequireAuthorization(Policies.Directorate);

        group.MapDelete("/shifts/{shiftId:guid}", async (Guid shiftId, IScheduleService service, CancellationToken ct) =>
            {
                await service.DeleteShiftAsync(shiftId, ct);
                return Results.NoContent();
            })
            .RequireAuthorization(Policies.Directorate);

        group.MapPost("/shifts/{shiftId:guid}/periods",
                async (Guid shiftId, ShiftPeriodWriteDto dto, IScheduleService service, CancellationToken ct) =>
                    Results.Ok(await service.AddPeriodAsync(shiftId, dto, ct)))
            .RequireAuthorization(Policies.Directorate);

        group.MapPut("/periods/{periodId:guid}",
                async (Guid periodId, ShiftPeriodWriteDto dto, IScheduleService service, CancellationToken ct) =>
                    Results.Ok(await service.UpdatePeriodAsync(periodId, dto, ct)))
            .RequireAuthorization(Policies.Directorate);

        group.MapDelete("/periods/{periodId:guid}", async (Guid periodId, IScheduleService service, CancellationToken ct) =>
            {
                await service.DeletePeriodAsync(periodId, ct);
                return Results.NoContent();
            })
            .RequireAuthorization(Policies.Directorate);

        // Entries ----------------------------------------------------------
        group.MapGet("/class/{classId:guid}", async (Guid classId, IScheduleService service, CancellationToken ct) =>
                Results.Ok(await service.GetClassTimetableAsync(classId, ct)))
            .RequireAuthorization(Policies.StudentReadable);

        group.MapGet("/teacher/{teacherId:guid}",
                async (Guid teacherId, IScheduleService service, CancellationToken ct) =>
                    Results.Ok(await service.GetTeacherTimetableAsync(teacherId, ct)))
            .RequireAuthorization(Policies.DirectorateOrTeacher);

        group.MapGet("/my", async (IScheduleService service, ICurrentUser currentUser, CancellationToken ct) =>
                Results.Ok(await service.GetTeacherTimetableAsync(currentUser.RequireTeacherId(), ct)))
            .RequireAuthorization(Policies.Teacher);

        group.MapPost("/entries", async (ScheduleEntryCreateDto dto, IScheduleService service, CancellationToken ct) =>
            {
                var entry = await service.CreateEntryAsync(dto, ct);
                return Results.Created($"/api/schedule/entries/{entry.Id}", entry);
            })
            .RequireAuthorization(Policies.Directorate);

        group.MapDelete("/entries/{entryId:guid}", async (Guid entryId, IScheduleService service, CancellationToken ct) =>
            {
                await service.DeleteEntryAsync(entryId, ct);
                return Results.NoContent();
            })
            .RequireAuthorization(Policies.Directorate);
    }
}
